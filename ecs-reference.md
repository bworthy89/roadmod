# Cities: Skylines II ECS Reference

## Systems

Systems are the active logic processors in the ECS architecture. They operate on entities with specific component combinations during their designated update phases.

---

### Core Systems

#### UpdateSystem (`Game`)

**Summary**: Calls other game systems at the requested `SystemUpdatePhase` and time.

**Description**:
Systems can be registered in the UpdateSystem for certain update phases (e.g., PreSimulation, GameSimulation, PostSimulation).

**Key Features**:
- Systems registered with `UpdateAt()`, `UpdateBefore()`, and `UpdateAfter()` methods
- One system can be registered multiple times for different phases
- Registered systems are grouped by phase using `m_UpdateRanges` lookup list
- Within one phase, systems execute in registration order (via `addIndex`)

**Update Intervals & Offsets**:
```csharp
public override int GetUpdateInterval(SystemUpdatePhase phase)
{
    // IMPORTANT: Must be a power of 2
    return 262144 / updatesPerDay;
}

public override int GetUpdateOffset(SystemUpdatePhase phase)
{
    return -1; // Default: uint.MaxValue after cast
}
```

**Critical Notes**:
- **Interval MUST be a power of 2**
- **Offset MUST be `0 <= offset < interval`**
- Uses bit masking: `updateIndex & (interval - 1) == offset` (NOT `updateIndex % interval == offset`)
- Default offset of `-1` becomes `uint.MaxValue` after cast → system won't update until iteration `uint.MaxValue`
- Intervals/offsets only apply to LoadSimulation, GameSimulation, and EditorSimulation phases

---

#### SimulationSystem (`Game.Simulation`)

**Summary**: Main entry point of the simulation.

**Description**:
Runs the simulation at high level and triggers updates of other systems via UpdateSystem.

**Update Sequence**:
1. Calculate update steps per frame (based on "Simulation vs. FrameRate performance" preference)
2. Execute UpdateSystem multiple times:
   - `UpdateSystem.Update(SystemUpdatePhase.PreSimulation)`
   - `UpdateSystem.Update(SystemUpdatePhase.GameSimulation / EditorSimulation)` (multiple times with incrementing `updateIndex` and `iterationIndex`)
   - `UpdateSystem.Update(SystemUpdatePhase.PostSimulation)`

**Parameters**:
- **updateIndex**: Constantly incrementing from game start
- **iterationIndex**: Reset to 0 for each frame

---

### Cleanup Systems

#### PrepareCleanUpSystem (`Game.Common`)

**Summary**: Collects deleted and updated entities for CleanUpSystem.

**Execution**: Runs right after MainLoop system update phase.

**Function**: Collects entities marked by `Game.Common.Deleted` or `Game.Common.Updated` tags.

---

#### CleanUpSystem (`Game.Common`)

**Summary**: Destroys deleted entities and cleans up updated entities.

**Execution**: Runs during CleanUp system update phase (after MainLoop and UI update phase).

**Operations**:
1. Destroys every "Deleted" entity (collected by PrepareCleanUpSystem)
2. Removes the following components from "Updated" entities:
   - `Game.Common.Created`
   - `Game.Common.Updated`
   - `Game.Common.Applied`
   - `Game.Common.EffectsUpdated`
   - `Game.Common.BatchesUpdated`
   - `Game.Common.PathfindUpdated`

---

### Network Systems

#### LaneSystem (`Game.Net`)

**Summary**: Updates road sections entities and their sub components with changes and deletions.

**Triggers on**: Entities with `SubLane` component + `Updated/Deleted` component, but WITHOUT:
- `OutsideConnection` (either `Game.Net` or `Game.Objects`)
- `Area` components

**Jobs**:
- **UpdateLanesJob**:
  - Deletes all SubLanes of deleted entities (except those with `SecondaryLaneData`)
  - Executes updates for entities with `Updated` component

---

#### SearchSystem (`Game.Net`)

**Summary**: Creates and updates two QuadTrees for quick bounds retrieval.

**QuadTrees**:
1. **LaneSearchTree** (`m_LaneSearchTree`): Bounds of Lanes
2. **NetSearchTree** (`m_NetSearchTree`): Bounds of Edges

**Jobs**:
- **UpdateLaneSearchTreeJob**:
  - Initial: All entities with `Game.Net.LaneGeometry` but no `Game.Tools.Temp`
  - Ongoing: Modified and deleted entities
- **UpdateNetSearchTreeJob**:
  - Initial: All entities with `Game.Net.Edge` and `Game.Net.Node` but no `Game.Tools.Temp`
  - Ongoing: Modified and deleted entities

**Access**: `GetLaneSearchTree()` and `GetNetSearchTree()` methods

---

### Pathfinding Systems

#### PathfindQueueSystem (`Game.PathFind`)

**Summary**: Maintains queues for path finding requests. New requests can be queued with `Enqueue()` methods, processed on multiple threads.

**Action Types**:
- **PathfindAction**: Finding a path between locations
- **CreateAction, UpdateAction, DeleteAction**: Maintain pathfinding graph
- **CoverageAction, AvailabilityAction**: Calculate coverage and availability for city services
- **DensityAction**: Update "density" on pathfinding graph
- **TimeAction**: Update "time costs" (e.g., traffic jam avoidance)
- **FlowAction**: Update "flow offset" on pathfinding graph

**Queue Types** (processed in order):
1. High Priority queue (urgent path finding requests)
2. Modification queue (pathfinding graph updates)
3. Normal queue (everything else)

**Jobs Scheduled**:
```
CreateAction   → CreateEdgesJob
UpdateAction   → UpdateEdgesJob
DeleteAction   → DeleteEdgesJob
DensityAction  → SetDensityJob
TimeAction     → SetTimeJob
FlowAction     → SetFlowJob
CoverageAction → CoverageJob (via PathfindWorkerJob)
AvailabilityAction → AvailabilityJob (via PathfindWorkerJob)
PathfindAction → PathfindJob (via PathfindWorkerJob)
```

**PathfindWorkerJob**:
- Bundles many actions together
- Scheduled on multiple threads simultaneously
- Number of threads: `(m_MaxThreadCount / 2) + m_HighPriorityCount`

**Performance Note**: For high population (100k+) cities, majority of CPU time is spent by PathfindWorkerJobs processing path finding requests.

---

### Navigation Systems

#### CarNavigationSystem (`Game.Simulation`)

**Summary**: Handles car navigation logic.

**Triggers on**: "Car" entities with:
- `Car`
- `CarCurrentLane`
- `UpdateFrame`

**Excludes**:
- `Deleted`
- `Temp`
- `ParkedCar`
- `TripSource`

**Jobs**:
- **UpdateNavigationJob**: Scheduled for all matching car entities

---

### Citizen Systems

#### CitizenBehaviorSystem (`Game.Simulation`)

**Summary**: Simulates citizen behavior (going home, shopping, leisure, traveling, sending mails, sleeping, etc.).

**Jobs**:

##### 1. CitizenAITickJob

**Targets**: "Citizen" entities with:
- `Game.Citizens.Citizen`
- `Game.Citizens.CurrentBuilding`
- `Game.Citizens.HouseholdMember`

**Excludes**:
- `Game.Citizens.TravelPurpose`
- `Game.Companies.ResourceBuyer`
- `Game.Common.Deleted`
- `Game.Tools.Temp`

**Logic Flow** (in order):

1. **Mark citizen Deleted if**:
   - No household or invalid household
   - Household is moving out
   - Commuter but not Adult

2. **MovingAway handling**:
   - Create `TripNeeded` to random outside connection
   - Set `CitizenFlags.MovingAwayReachOC` if at outside connection
   - Remove `Leisure`, `Worker`, `Student` components

3. **Health problems**: Remove `Leisure` component

4. **Attending meeting** (`AttendingMeeting` component):
   - **Shopping**: Add to car reserve queue, add `ResourceBuyer` component
   - **Traveling**: Create `TripNeeded` to outside connection
   - **GoingHome**: Add to car reserve + mail sender queue, create `TripNeeded` to home
   - **Other purposes**: Create `TripNeeded` with meeting details

5. **Work/Study time**: Remove `Leisure` component

6. **Sleep time**:
   - If not at home: Create `TripNeeded` with `GoingHome`
   - If at home: Add to `SleepQueue`, add `TravelPurpose.Sleeping`, release car

7. **Household needs**: Add `ResourceBuyer` component if household needs resources

8. **Leisure decision**:
   - Calculate remaining time until Sleep/Work/Study
   - Add `Leisure` component with timeframe
   - If no leisure: Send home or release car if already home

##### 2. CitizenReserveHouseholdCarJob

**Queue**: CarReserveQueue

**Function**: Links citizen to household car if:
- Citizen age != 0
- Household has unreserved car (`PersonalCar.m_Keeper == Entity.Null`)
- Citizen has no car reserved (`CarKeeper` disabled)

**Result**: Sets `CarKeeper.m_Car` and `PersonalCar.m_Keeper`

##### 3. CitizenTryCollectMailJob

**Queue**: MailSenderQueue

**Function**: Assign mail collection to citizens

**Conditions**:
- Current building has `MailProducer` with `m_SendingMail > 15`
- Building requires mail collecting (prefab has `MailAccumulationData.m_RequireCollect = true`)

**Calculation**:
- Max mails per citizen: 100 - current `MailSender.m_Amount`
- Assign min(building's `m_SendingMail`, citizen capacity)

**Result**: Enable `MailSender`, update amounts

##### 4. CitizenSleepJob

**Queue**: SleepQueue

**Function**: Decrement `CitizenPresence.m_Delta` by 1 for current building

---

#### CitizenFindJobSystem (`Game.Simulation`)

**Summary**: Searches for citizens who should be looking for a job.

**Jobs**:

##### 1. CitizenFindJobJob (Unemployed mode)

**Triggers on**: Entities with:
- `Game.Citizens.Citizen`
- `Game.Citizens.HouseholdMember`

**Excludes**:
- `Game.Tools.Temp`
- `Game.Citizens.Worker`
- `Game.Citizens.Student`
- `Game.Agents.HasJobSeeker`
- `Game.Citizens.HasSchoolSeeker`
- `Game.Citizens.HealthProblem`
- `Game.Common.Deleted`

**Logic**: Mark as "job seeker" unless available workplaces on educational level < 0~100 (random)

##### 2. CitizenFindJobJob (Employed mode)

**Triggers on**: Entities with:
- `Game.Citizens.Citizen`
- `Game.Citizens.HouseholdMember`
- `Game.Citizens.Worker`

**Excludes**: Same as unemployed mode + `Game.Citizens.Student`

**Scheduling**: Random, influenced by `CitizenParametersData.m_SwitchJobRate`

**Logic**: Calculate available jobs on current level or higher. Exclude if total < 0~100 (random)

**Job Seeker Entity Creation**:
```csharp
// Created components:
Game.Common.Owner         // References citizen entity
Game.Agents.JobSeeker     // Contains citizen's education level
Game.Citizens.CurrentBuilding  // References citizen's home

// Citizen entity updated:
Game.Agents.HasJobSeeker  // Enabled, references job seeker entity
```

**Note**: `HasJobSeeker` set to null if excluded due to insufficient workplaces.

---

#### CommuterSpawnSystem (`Game.Simulation`)

**Summary**: Creates "foreign" households commuting to the city to work.

**Trigger**: If commuter households < 12.5% of workers

**Job**: SpawnCommuterHouseholdJob

**Logic**:
1. Calculate free workplaces (Educated or above)
2. Subtract employable citizens (same educational levels)
3. Take 12.5% of result
4. Create exactly that many commuter households

**Commuter Household Components**:
```csharp
Game.Prefabs.PrefabRef      // Random household prefab
Game.Citizens.Household     // New household with Commuter flag
Game.Citizens.CurrentBuilding  // Random outside connection
Game.Citizens.CommuterHousehold  // Records origin (same outside connection)
```

---

#### FindJobSystem (`Game.Simulation`)

**Summary**: Finds new workplaces for citizens marked as job seekers.

**Jobs**:

##### 1. FindJobJob

**Function**: Queue pathfinding queries for job seekers

**Process**:
1. Add `PathInformation` component with Pending state
2. Schedule pathfinding with Pedestrian + PublicTransport methods only (NO car)

**Note**: Strange that car ("Road") method is not included

##### 2. StartWorkingJob

**Function**: Handle job seekers with finished pathfinding (non-pending `PathInformation`)

**If workplace has open jobs**:
1. Create `Game.Company.Employee` component → add to Company
2. Create `Game.Citizens.Worker` component → add to citizen with employment details
3. Create `CitizenStartedWorking` TriggerAction
4. Delete old `Worker` component if exists
5. Update new company's `FreeWorkplaces`

**Note**: Old company's `FreeWorkplaces` NOT updated by this job (unclear if updated elsewhere)

**Cleanup**: Delete JobSeeker entity (`Game.Common.Deleted`)

---

#### WorkerSystem (`Game.Simulation`)

**Summary**: Sets GoingToWork TravelPurpose and handles unemployed citizens when workplace unavailable.

**Jobs**:

##### 1. GoToWorkJob

**Targets**: Citizens with:
- `Game.Citizens.Worker`
- `Game.Citizens.Citizen`
- `Game.Citizens.CurrentBuilding`

**Excludes**:
- `Game.Citizens.HealthProblem`
- `Game.Citizens.TravelPurpose`
- `Game.Companies.ResourceBuyer`

**Logic**:
1. Check if "time to work" (depends on day/night shift)
2. Check if not "day off"
3. Verify workplace building exists (`Game.Buildings.Building` or `Game.Objects.OutsideConnection`)
4. If valid: Create `TripNeeded` with `GoingToWork` purpose
5. If invalid: Remove `Worker` component, queue `CitizenBecameUnemployed` TriggerAction

##### 2. WorkJob

**Targets**: Citizens with:
- `Game.Citizens.Worker`
- `Game.Citizens.Citizen`
- `Game.Citizens.CurrentBuilding`
- `Game.Citizens.TravelPurpose`

**Logic**:
1. If workplace has no `WorkProvider`: Set unemployed (same as GoToWorkJob)
2. If `TravelPurpose == Working` AND (not work time OR has meeting): Remove `Working` TravelPurpose

---

### Resource & Commerce Systems

#### ResourceBuyerSystem (`Game.Simulation`)

**Summary**: Schedules pathfinding to find nearby shops for citizens/companies to buy resources, handles transactions when seller found.

**Jobs**:

##### 1. HandleBuyersJob

**Part A: Entities with ResourceBought**

**Targets**: Entities with:
- `Game.Citizens.ResourceBought`

**Excludes**:
- `Game.Common.Deleted`
- `Game.Tools.Temp`

**Logic**:
1. If buyer and seller both have `PrefabRef`: Create `SalesEvent`, enqueue to `SalesQueue`
2. Remove `ResourceBought` component

**Part B: Entities with ResourceBuyer**

**Targets**: Entities with:
- `Game.Companies.ResourceBuyer`
- `Game.Citizens.TripNeeded`

**Excludes**:
- `Game.Citizens.TravelPurpose`
- `Game.Common.Deleted`
- `Game.Tools.Temp`

**Special Case** (Outside Connection Import):
If:
- Has `CurrentBuilding` = outside connection
- No household OR normal (not tourist/commuter) household
- No `AttendingMeeting`

Then: Queue `SalesEvent` with `ImportFromOC` flag, remove `ResourceBuyer`

**Phase 1: Schedule Pathfinding**

**For Citizens**:
```csharp
MaxSpeed: 277.77
WalkSpeed: From citizen's prefab (HumanData.m_WalkSpeed)
Weights:
  Time: 1.25 (max leisure) to 20.0 (min leisure)
  Behavior: 2.0
  Money: citizens_in_household / household_daily_consumption
  Comfort: Random 1.0 to 3.0
Methods: Pedestrian, Taxi, PublicTransport
Origin: CurrentLocation
Destination: ResourceSeller with needed amount
PathfindFlags: SkipPathfind if resource is virtual
```

**If citizen has parked car** (`CarKeeper` + `ParkedCar`):
- MaxSpeed: Car's maximum speed
- ParkingTarget, ParkingDelta: Parked car's location
- ParkingSize, IgnoredRules: Based on car (e.g., EV can enter combustion-prohibited zones)
- Methods: Add Road + Parking

**For Companies**:
```csharp
MaxSpeed: 111.11
WalkSpeed: 5.55
Weights:
  Time: 1.0
  Behavior: 1.0
  Money: Based on resource amount and weight
  Comfort: 1.0
Methods: Road, CargoLoading
Origin: CurrentLocation
Destination: ResourceSeller with needed amount
PathfindFlags: SkipPathfind if resource is virtual
```

**Phase 2: Process Finished Pathfinding**

**Conditions**:
- `PathInformation` not Pending
- Destination is `PropertyRenter` or `OutsideConnection`
- Has enough resource (if virtual OR needed < 2× available)

**Actions**:
1. Create `SalesEvent`:
   - Resource type, amount
   - Buyer, Seller (destination), Distance
   - Flags: Virtual, CommercialSeller (if has `ServiceAvailable`), ImportFromOC
2. Enqueue to `SalesQueue`

**Real Trip Decision**:
- Based on random factors + `TrafficReduction` + population
- If needed: Add `TripNeeded` with Shopping/CompanyShopping purpose
- If meeting was Shopping: Set meeting status to Done

**Cleanup**: Remove `PathInformation`, `PathElement`, `ResourceBuyer`

##### 2. BuyJob

**Function**: Process Sales Events from HandleBuyersJob

**Price Calculation**:
```csharp
// Base price
If seller is factory: ResourceData.m_Price.x
If seller is shop: ResourceData.m_Price.x + ResourceData.m_Price.y (profit)

// Trade costs (simulate supply & demand)
Price += seller.BuyTradeCost * amount
If seller is commercial: seller.SellTradeCost = halfway between original and buyer.SellTradeCost + transport_cost
If buyer in city: buyer.BuyTradeCost = halfway between original and seller.BuyTradeCost + transport_cost

// Transport cost
From distance, resource weight, amount
```

**Skip if**: Seller has no resource

**Seller Updates** (if commercial):
```csharp
ServiceAvailable.m_ServiceAvailable -= resource_amount
ServiceAvailable.m_MeanPriority -= (reflects reduced availability)
```

**Resource Transfer**:
- Seller's resource: Decrement by amount
- If buyer is citizen: Household wealth INCREASES by price (strange!)
- If buyer is company: Update last trade partner, add resource if virtual
- Add price to seller's account (money resource)

**Vehicle Purchase**:
If resource is vehicle: Create new unspawned vehicle at seller location, add to household as `OwnedVehicle`

**Notes**:
- **Strange #1**: Price NOT deducted from buyer's account
- **Strange #2**: Resource NOT added to household or (if not virtual) buying company's resources

---

## Components

Components are data containers attached to entities. They define what data an entity has.

### Agent Components

#### HasJobSeeker (`Game.Agents`)

**Type**: Unmanaged, Enableable

**Description**: Reference to a "job seeker" entity linked to citizens looking for a job.

| Property | Type | Description |
|----------|------|-------------|
| `m_Seeker` | Entity | The JobSeeker entity |
| `m_LastJobSeekFrameIndex` | uint | - |

---

#### JobSeeker (`Game.Agents`)

**Type**: Unmanaged

**Description**: Used in "job seeker" entities to find new job for citizen.

| Property | Type | Description |
|----------|------|-------------|
| `m_Level` | byte | Current educational level of job-seeking citizen |
| `m_Outside` | byte | - |

---

#### MovingAway (`Game.Agents`)

**Type**: Unmanaged

**Description**: Marks a citizen leaving the city.

| Property | Type | Description |
|----------|------|-------------|
| `m_Target` | Entity | The "outside connection" entity used to leave |

---

### Building Components

#### Building (`Game.Buildings`)

**Type**: Unmanaged

**Description**: A building's location and other properties.

| Property | Type | Description |
|----------|------|-------------|
| `m_RoadEdge` | Entity | Road segment where building connects to road network |
| `m_CurvePosition` | float | Position of road connection within road segment |
| `m_OptionMask` | uint | - |
| `m_Flags` | BuildingFlags | HighRent, StreetLightsOff, LowEfficiency, Illuminated |

---

#### PropertyRenter (`Game.Buildings`)

**Type**: Unmanaged

**Description**: Entity (household or company) is renting a property.

| Property | Type | Description |
|----------|------|-------------|
| `m_Property` | Entity | The rented property (building entity) |
| `m_Rent` | int | Cost of rent |

---

### City Components

#### CityServiceUpkeep (`Game.City`)

**Type**: Unmanaged (Tag)

**Description**: Marks city service buildings.

---

### Citizen Components

#### CarKeeper (`Game.Citizens`)

**Type**: Unmanaged, Enableable

**Description**: Link to the citizen's car (if exists).

| Property | Type | Description |
|----------|------|-------------|
| `m_Car` | Entity | Reference to citizen's car entity |

---

#### Arrived (`Game.Citizens`)

**Type**: Unmanaged, Enableable (Tag)

**Description**: Marks citizen arrived to the city (?).

---

#### AttendingMeeting (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Reference to a "meeting" entity.

| Property | Type | Description |
|----------|------|-------------|
| `m_Meeting` | Entity | The "meeting" entity that citizen is attending |

---

#### Citizen (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Details of a citizen.

| Property | Type | Description |
|----------|------|-------------|
| `m_PseudoRandom` | ushort | Random seed for randomized citizen actions/events |
| `m_State` | CitizenFlags | MovingAwayReachOC, Tourist, Commuter, etc. |
| `m_WellBeing` | byte | Current "well being" |
| `m_Health` | byte | Current health |
| `m_LeisureCounter` | byte | How much leisure the citizen had |
| `m_PenaltyCounter` | byte | - |
| `m_UnemploymentCounter` | int | - |
| `m_BirthDay` | short | - |

---

#### CommuterHousehold (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Origin of household not living in city but commuting from abroad.

| Property | Type | Description |
|----------|------|-------------|
| `m_OriginalFrom` | Entity | Where commuter household commutes from (usually edge of map "arrow") |

---

#### CoordinatedMeeting (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Properties of a citizen meeting.

| Property | Type | Description |
|----------|------|-------------|
| `m_Status` | MeetingStatus | Waiting, Travelling, Attending, Done |
| `m_Phase` | int | - |
| `m_Target` | Entity | Building entity where meeting is held |
| `m_PhaseEndTime` | uint | - |

---

#### CurrentBuilding (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Reference to building where household is living.

| Property | Type | Description |
|----------|------|-------------|
| `m_CurrentBuilding` | Entity | Reference to building entity |

---

#### CurrentTransport (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Reference to a "current transport" entity.

| Property | Type | Description |
|----------|------|-------------|
| `m_CurrentTransport` | Entity | "Current transport" entity describing citizen's travel |

---

#### HealthProblem (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Current health problems of a citizen.

| Property | Type | Description |
|----------|------|-------------|
| `m_Event` | Entity | - |
| `m_HealthcareRequest` | Entity | - |
| `m_Flags` | HealthProblemFlags | Sick, Dead, Injured, RequireTransport, etc. |
| `m_Timer` | byte | - |

---

#### HomelessHousehold (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Household without a building to live.

| Property | Type | Description |
|----------|------|-------------|
| `m_TempHome` | Entity | Temporal home (e.g., a park) |

---

#### Household (`Game.Citizens`)

**Type**: Unmanaged

**Description**: General properties of a household.

| Property | Type | Description |
|----------|------|-------------|
| `m_Flags` | HouseholdFlags | Tourist, Commuter, MovedIn |
| `m_Resources` | int | Wealth of household. INCREASES when buying, DECREASES daily by consumption |
| `m_ConsumptionPerDay` | short | Daily consumption. Wealthier/larger households consume more |

---

#### HouseholdMember (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Reference to the "household" entity.

| Property | Type | Description |
|----------|------|-------------|
| `m_Household` | Entity | The "household" entity |

---

#### HouseholdNeed (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Temporarily marks a "need" for a household.

| Property | Type | Description |
|----------|------|-------------|
| `m_Resource` | Resource | Wood, Timber, Paper, Furniture, Food, Vehicles, etc. |
| `m_Amount` | int | Amount needed |

---

#### TravelPurpose (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Information about why a citizen is traveling.

| Property | Type | Description |
|----------|------|-------------|
| `m_Purpose` | Purpose | Shopping, Working, Hospital, Delivery, Sightseeing, GoingHome, GoingToWork, Crime, etc. |
| `m_Data` | int | - |
| `m_Resource` | Resource | Money, Food, Furniture, Beverages, etc. |

---

#### TripNeeded (`Game.Citizens`)

**Type**: Unmanaged, Buffer

**Description**: Places where the citizen should go.

| Property | Type | Description |
|----------|------|-------------|
| `m_TargetAgent` | Entity | Target entity (workplace building, outside connection) |
| `m_Purpose` | Game.Citizens.Purpose | Shopping, Leisure, GoingHome, GoingToWork, Working, Sleeping, MovingAway, GoingToSchool, Studying, Hospital, InHospital, EmergencyShelter, InEmergencyShelter, Crime, GoingToJail, InJail, GoingToPrison, InPrison, Traveling, Relaxing, Sightseeing, VisitAttractions, WaitingHome, PathFailed, etc. |
| `m_Data` | int | - |
| `m_Resource` | Resource | - |

---

#### TouristHousehold (`Game.Citizens`)

**Type**: Unmanaged

**Description**: A tourist household visiting the city.

| Property | Type | Description |
|----------|------|-------------|
| `m_Hotel` | Entity | The "hotel" entity where tourist is staying |
| `m_LeavingTime` | uint | Time when tourist household will leave |

---

#### Worker (`Game.Citizens`)

**Type**: Unmanaged

**Description**: Employment details of a citizen.

| Property | Type | Description |
|----------|------|-------------|
| `m_Workplace` | Entity | "Workplace" entity (company where citizen works) |
| `m_LastCommuteTime` | float | How long last commute took. Used to calculate "time to go to work" |
| `m_Level` | byte | Educational level of current employment |
| `m_Shift` | Workshift | Day or night |

---

### Common Components

#### Applied (`Game.Common`)

**Type**: Unmanaged (Tag)

**Description**: Marks applied entities (?).

---

#### BatchesUpdated (`Game.Common`)

**Type**: Unmanaged (Tag)

**Description**: Marks entities where graphics need to be updated.

---

#### Created (`Game.Common`)

**Type**: Unmanaged (Tag)

**Description**: Marks newly created entities.

---

#### Deleted (`Game.Common`)

**Type**: Unmanaged (Tag)

**Description**: Marks deleted entities.

---

#### Destroyed (`Game.Common`)

**Type**: Unmanaged

**Description**: Added to entities representing destroyed structures (e.g., collapsed building).

| Property | Type | Description |
|----------|------|-------------|
| `m_Event` | Entity | Reference to "event" entity that destroyed it |
| `m_Cleared` | float | Progress of cleanup work |

---

#### Event (`Game.Common`)

**Type**: Unmanaged (Tag)

**Description**: Marks "event" entities.

---

#### Overridden (`Game.Common`)

**Type**: Unmanaged (Tag)

**Description**: Marks objects conflicting with other objects/networks but not permanently deleted. Not shown unless ShowMarkers enabled via DevUI. Removed when conflicting object/network is removed.

---

#### Owner (`Game.Common`)

**Type**: Unmanaged

**Description**: Generic "owner" entity.

| Property | Type | Description |
|----------|------|-------------|
| `m_Owner` | Entity | Reference to entity owning current one |

---

#### PathfindUpdated (`Game.Common`)

**Type**: Unmanaged (Tag)

**Description**: Marks road lanes when pathfinding parameters updated.

---

#### PointOfInterest (`Game.Common`)

**Type**: Unmanaged

**Description**: A point of interest.

| Property | Type | Description |
|----------|------|-------------|
| `m_Position` | float3 | 3D coordinates of POI |
| `m_IsValid` | bool | - |

---

#### PseudoRandomSeed (`Game.Common`)

**Type**: Unmanaged

**Description**: Random seed for entities (e.g., light timing/brightness). Stored in saves for deterministic simulation.

| Property | Type | Description |
|----------|------|-------------|
| `m_Seed` | ushort | Stored seed for random event/action generation |

---

#### RandomLocalizationIndex (`Game.Common`)

**Type**: Unmanaged, Buffer

| Property | Type | Description |
|----------|------|-------------|
| `m_Index` | int | - |

---

#### Target (`Game.Common`)

**Type**: Unmanaged

**Description**: Generic "target" entity reference (destination for vehicle/person, meeting location, etc.).

| Property | Type | Description |
|----------|------|-------------|
| `m_Target` | Entity | Target entity (building, outside connection, company, etc.) |

---

#### TimeData (`Game.Common`)

**Type**: Unmanaged

| Property | Type | Description |
|----------|------|-------------|
| `m_FirstFrame` | uint | - |
| `m_StartingYear` | int | - |
| `m_StartingMonth` | byte | - |
| `m_StartingHour` | byte | - |
| `m_StartingMinutes` | byte | - |

---

#### Updated (`Game.Common`)

**Type**: Unmanaged (Tag)

**Description**: Entities with changed components (not just visual). E.g., Transform position changes, Overriden, CullingInfo, Leveling Up, Attached to roads. Updates trigger sub elements to be destroyed and recreated.

---

### Company Components

#### Employee (`Game.Companies`)

**Type**: Unmanaged, Buffer

**Description**: A resident working for a company.

| Property | Type | Description |
|----------|------|-------------|
| `m_Worker` | Entity | Reference to resident entity |
| `m_Level` | byte | Educational level of employee |

---

#### FreeWorkplaces (`Game.Companies`)

**Type**: Unmanaged

**Description**: Available workspaces per education level. `Refresh()` method recalculates based on building type/level and subtracts actual employees.

| Property | Type | Description |
|----------|------|-------------|
| `m_Uneducated` | byte | - |
| `m_PoorlyEducated` | byte | - |
| `m_Educated` | byte | - |
| `m_WellEducated` | byte | - |
| `m_HighlyEducated` | byte | - |

---

#### WorkProvider (`Game.Companies`)

**Type**: Unmanaged

**Description**: A company that creates jobs in the city.

| Property | Type | Description |
|----------|------|-------------|
| `m_MaxWorkers` | int | - |
| `m_UneducatedCooldown` | short | - |
| `m_EducatedCooldown` | short | - |
| `m_UneducatedNotificationEntity` | Entity | - |
| `m_EducatedNotificationEntity` | Entity | - |
| `m_EfficiencyCooldown` | short | - |

---

### Creature Components

#### CurrentVehicle (`Game.Creatures`)

**Type**: Unmanaged

**Description**: Used in "current transport" entities to reference vehicle.

| Property | Type | Description |
|----------|------|-------------|
| `m_Vehicle` | Entity | Reference to "vehicle" entity |
| `m_Flags` | CreatureVehicleFlags | Ready, Leader, Driver, Entering, Exiting |

---

### Tool Components

#### Temp (`Game.Tools`)

**Type**: Unmanaged

**Description**: Marks entities about to be created/updated but not finalized (e.g., road drawn but not clicked to create).

| Property | Type | Description |
|----------|------|-------------|
| `m_Original` | Entity | - |
| `m_CurvePosition` | float | - |
| `m_Value` | int | - |
| `m_Cost` | int | - |
| `m_Flags` | TempFlags | - |

---

#### Highlighted (`Game.Tools`)

**Type**: Unmanaged (Tag)

**Description**: Entities highlighted in game with white outline.

---

### Network Components

#### CarLane (`Game.Net`)

**Type**: Unmanaged

**Description**: Properties of an individual lane of a road.

| Property | Type | Description |
|----------|------|-------------|
| `m_AccessRestriction` | Entity | - |
| `m_Flags` | CarLaneFlags | Highway, TurnLeft, TurnRight, ParkingLeft, Yield, Stop, ForbidPassing, etc. |
| `m_DefaultSpeedLimit` | float | Default speed limit of lane type |
| `m_SpeedLimit` | float | Current speed limit (can differ from default, e.g., High Speed Highways policy) |
| `m_Curviness` | float | Higher = more curvy |
| `m_CarriagewayGroup` | ushort | - |
| `m_BlockageStart` | byte | - |
| `m_BlockageEnd` | byte | - |
| `m_CautionStart` | byte | - |
| `m_CautionEnd` | byte | - |
| `m_FlowOffset` | byte | - |
| `m_LaneCrossCount` | byte | - |

---

#### Composition (`Game.Net`)

**Type**: Unmanaged

**Description**: An Edge with its start and end Node.

| Property | Type | Description |
|----------|------|-------------|
| `m_Edge` | Entity | The Edge entity reference |
| `m_StartNode` | Entity | The StartNode entity reference |
| `m_EndNode` | Entity | The EndNode entity reference |

---

#### Curve (`Game.Net`)

**Type**: Unmanaged

**Description**: A bezier curve and its length.

| Property | Type | Description |
|----------|------|-------------|
| `m_Bezier` | Bezier4x3 | The curve |
| `m_Length` | float | The length of the curve |

---

#### Edge (`Game.Net`)

**Type**: Unmanaged

**Description**: A road segment between two nodes.

| Property | Type | Description |
|----------|------|-------------|
| `m_Start` | Entity | Start node |
| `m_End` | Entity | End node |

---

#### EdgeGeometry (`Game.Net`)

**Type**: Unmanaged

**Description**: The outlines of an Edge.

| Property | Type | Description |
|----------|------|-------------|
| `m_Start` | Segment | Segment between start Node and middle of Edge |
| `m_End` | Segment | Segment between middle of Edge and end Node |
| `m_Bounds` | Bounds3 | Bounding box of Edge |

---

#### Lane (`Game.Net`)

**Type**: Unmanaged

| Property | Type | Description |
|----------|------|-------------|
| `m_StartNode` | PathNode | - |
| `m_MiddleNode` | PathNode | - |
| `m_EndNode` | PathNode | - |

---

#### Node (`Game.Net`)

**Type**: Unmanaged

**Description**: Road node (intersection or simple control point).

| Property | Type | Description |
|----------|------|-------------|
| `m_Position` | float3 | Position of node |
| `m_Rotation` | quaternion | Rotation of node |

---

#### Road (`Game.Net`)

**Type**: Unmanaged

**Description**: Other road properties.

| Property | Type | Description |
|----------|------|-------------|
| `m_TrafficFlowDuration0` | float4 | - |
| `m_TrafficFlowDuration1` | float4 | - |
| `m_TrafficFlowDistance0` | float4 | - |
| `m_TrafficFlowDistance1` | float4 | - |
| `m_Flags` | RoadFlags | StartHalfAligned, EndHalfAligned, IsLit, AlwaysLit, LightsOff |

---

#### Roundabout (`Game.Net`)

**Type**: Unmanaged

**Description**: Marks a node as a roundabout.

| Property | Type | Description |
|----------|------|-------------|
| `m_Radius` | float | Radius of roundabout |

---

#### SubLane (`Game.Net`)

**Type**: Unmanaged, Buffer

**Description**: Every sub part of a road: car lanes, entrance lanes, parking lanes, road markings, pedestrian walkways, pipes, cables, etc.

| Property | Type | Description |
|----------|------|-------------|
| `m_SubLane` | Entity | Reference to "sublane" entity |
| `m_PathMethods` | PathMethod | Pedestrian, Road, Parking, PublicTransportDay, Track, Taxi, CargoTransport, CargoLoading, Flying, PublicTransportNight, Boarding, Offroad |

---

#### SubNet (`Game.Net`)

**Type**: Unmanaged, Buffer

**Description**: Reference to a "subnet" entity.

| Property | Type | Description |
|----------|------|-------------|
| `m_SubNet` | Entity | Reference to "subnet" entity |

---

#### OutsideConnection (`Game.Net`)

**Type**: Unmanaged

| Property | Type | Description |
|----------|------|-------------|
| `m_Delay` | float | - |

---

### Object Components

#### Moving (`Game.Objects`)

**Type**: Unmanaged

**Description**: Current velocity and rotation of an object.

| Property | Type | Description |
|----------|------|-------------|
| `m_Velocity` | float3 | - |
| `m_AngularVelocity` | float3 | - |

---

#### TripSource (`Game.Objects`)

**Type**: Unmanaged

| Property | Type | Description |
|----------|------|-------------|
| `m_Source` | Entity | - |
| `m_Timer` | int | - |

---

#### OutsideConnection (`Game.Objects`)

**Type**: Unmanaged (Tag)

**Description**: Marks outside connections.

---

### Pathfind Components

#### PathElement (`Game.Pathfind`)

**Type**: Unmanaged, Buffer

**Description**: Buffer element storing steps (road segments) of previously planned route.

| Property | Type | Description |
|----------|------|-------------|
| `m_Target` | Entity | Reference to SubLane entity |
| `m_TargetDelta` | float2 | Used interval within SubLane (or 0-1 if whole SubLane used) |
| `m_Flags` | PathElementFlags | Secondary, PathStart, Action, Return, Reverse, WaitPosition, Leader |

---

#### PathInformation (`Game.Pathfind`)

**Type**: Unmanaged

**Description**: Pathfinding information.

| Property | Type | Description |
|----------|------|-------------|
| `m_Origin` | Entity | Pathfinding from |
| `m_Destination` | Entity | Pathfinding to |
| `m_Distance` | float | Distance |
| `m_Duration` | float | Duration |
| `m_TotalCost` | float | Total cost |
| `m_Methods` | PathMethod | Pedestrian, Road, PublicTransport, Taxi, Cargo, Flying, etc. |
| `m_State` | PathFlags | Pending, Failed, Obsolete, Scheduled, Updated, Stuck |

---

#### PathOwner (`Game.Pathfind`)

**Type**: Unmanaged

| Property | Type | Description |
|----------|------|-------------|
| `m_ElementIndex` | int | - |
| `m_State` | PathFlags | Pending, Failed, Obsolete, Scheduled, Append, Updated, Stuck, WantsEvent, AddDestination, Debug, Divert, DivertObsolete, CachedObsolete |

---

### Prefab Components

#### CarData (`Game.Prefab`)

**Type**: Unmanaged

**Description**: Capabilities of a car (max speed, braking, turning, etc.).

| Property | Type | Description |
|----------|------|-------------|
| `m_SizeClass` | SizeClass | Small, medium, large |
| `m_EnergyType` | EnergyTypes | Gas, Electric, Hybrid |
| `m_MaxSpeed` | float | Maximum speed |
| `m_Acceleration` | float | Maximum acceleration |
| `m_Braking` | float | Maximum braking |
| `m_PivotOffset` | float | Turning radius of car |
| `m_Turning` | float2 | How much car needs to slow down in curve |

---

### Simulation Components

#### Dispatched (`Game.Simulation`)

**Type**: Unmanaged

| Property | Type | Description |
|----------|------|-------------|
| `m_Handler` | Entity | - |

---

#### UpdateFrame (`Game.Simulation`)

**Type**: Unmanaged, Shared

| Property | Type | Description |
|----------|------|-------------|
| `m_Index` | uint | - |

---

#### ServiceDispatch (`Game.Simulation`)

**Type**: Unmanaged, Buffer

**Description**: Reference to a "service dispatch" entity.

| Property | Type | Description |
|----------|------|-------------|
| `m_Request` | Entity | Reference to "service dispatch" entity (e.g., taxi call) |

---

#### ServiceRequest (`Game.Simulation`)

**Type**: Unmanaged

| Property | Type | Description |
|----------|------|-------------|
| `m_FailCount` | byte | - |
| `m_Cooldown` | byte | - |
| `m_Flags` | ServiceRequestFlags | Reversed, SkipCooldown |

---

#### TaxiRequest (`Game.Simulation`)

**Type**: Unmanaged

**Description**: Properties of a taxi request.

| Property | Type | Description |
|----------|------|-------------|
| `m_Seeker` | Entity | Citizen who called taxi |
| `m_District1` | Entity | - |
| `m_District2` | Entity | - |
| `m_Priority` | int | - |
| `m_Type` | TaxiRequestType | Stand, Customer, Outside |

---

### Vehicle Components

#### Blocker (`Game.Vehicles`)

**Type**: Unmanaged

**Description**: Reference to entity blocking current entity's movement.

| Property | Type | Description |
|----------|------|-------------|
| `m_Blocker` | Entity | Entity blocking current entity |
| `m_Type` | BlockerType | Continuing (moving but slowing), Signal (red light), etc. |
| `m_MaxSpeed` | byte | - |

---

#### Car (`Game.Vehicles`)

**Type**: Unmanaged

**Description**: Car properties.

| Property | Type | Description |
|----------|------|-------------|
| `m_Flags` | CarFlags | Emergency, queueing, using PT lanes, etc. |

---

#### CarCurrentLane (`Game.Vehicles`)

**Type**: Unmanaged

**Description**: Reference to lane where car currently is.

| Property | Type | Description |
|----------|------|-------------|
| `m_Lane` | Entity | - |
| `m_ChangeLane` | Entity | - |
| `m_CurvePosition` | float3 | - |
| `m_LaneFlags` | CarLaneFlags | - |
| `m_ChangeProgress` | float | - |
| `m_Duration` | float | - |
| `m_Distance` | float | - |
| `m_LanePosition` | float | - |

---

#### CarNavigation (`Game.Vehicles`)

**Type**: Unmanaged

| Property | Type | Description |
|----------|------|-------------|
| `m_TargetPosition` | float3 | - |
| `m_TargetRotation` | quaternion | - |
| `m_MaxSpeed` | float | - |

---

#### CarNavigationLane (`Game.Vehicles`)

**Type**: Unmanaged, Buffer

| Property | Type | Description |
|----------|------|-------------|
| `m_Lane` | Entity | - |
| `m_CurvePosition` | float2 | - |
| `m_Flags` | CarLaneFlags | - |

---

#### Odometer (`Game.Vehicles`)

**Type**: Unmanaged

**Description**: Total distance taken by vehicle.

| Property | Type | Description |
|----------|------|-------------|
| `m_Distance` | float | Total distance taken by vehicle |

---

#### OwnedVehicle (`Game.Vehicles`)

**Type**: Unmanaged, Buffer

**Description**: Vehicles owned by a household.

| Property | Type | Description |
|----------|------|-------------|
| `m_Vehicle` | Entity | Reference to owner vehicle entity |

---

#### ParkedCar (`Game.Vehicles`)

**Type**: Unmanaged

**Description**: Marks a car parking.

| Property | Type | Description |
|----------|------|-------------|
| `m_Lane` | Entity | Reference to lane entity where car is parking |
| `m_CurvePosition` | float | Position within parking lane |

---

#### Passenger (`Game.Vehicles`)

**Type**: Unmanaged, Buffer

**Description**: Buffer for referencing passengers of a vehicle.

| Property | Type | Description |
|----------|------|-------------|
| `m_Passenger` | Entity | The passenger entity |

---

#### PersonalCar (`Game.Vehicles`)

**Type**: Unmanaged

**Description**: Properties of personally owned car (not taxi, police car, etc.).

| Property | Type | Description |
|----------|------|-------------|
| `m_Keeper` | Entity | Reference to "citizen" entity owning the car |
| `m_State` | PersonalCarFlags | Transporting, Boarding, Disembarking, DummyTraffic, HomeTarget |

---

#### Taxi (`Game.Vehicles`)

**Type**: Unmanaged

| Property | Type | Description |
|----------|------|-------------|
| `m_TargetRequest` | Entity | - |
| `m_State` | TaxiFlags | - |
| `m_PathElementTime` | float | - |
| `m_StartDistance` | float | - |
| `m_MaxBoardingDistance` | float | - |
| `m_MinWaitingDistance` | float | - |
| `m_ExtraPathElementCount` | int | - |
| `m_NextStartingFee` | ushort | - |
| `m_CurrentFee` | ushort | - |

---

#### Vehicles (`Game.Vehicles`)

**Type**: Unmanaged (Tag)

**Description**: Vehicle tag.

---

## Other Structs

### Bezier4x3 (`Colossal.Mathematics`)

**Description**: A three dimensional 4 point bezier curve.

| Property | Type | Description |
|----------|------|-------------|
| `a` | float3 | Start of curve |
| `b` | float3 | First control point |
| `c` | float3 | Second control point |
| `d` | float3 | End of curve |

---

### Segment (`Game.Net`)

**Description**: A road segment between a Node and the middle of an Edge.

| Property | Type | Description |
|----------|------|-------------|
| `m_Left` | Bezier4x3 | Left outline of Segment |
| `m_Right` | Bezier4x3 | Right outline of Segment |
| `m_Length` | float2 | Length of Segment |

---

## Quick Reference

### Game Time
- **One in-game day/month**: 262,144 ticks

### Critical Constraints
- **Update intervals**: MUST be power of 2
- **Update offsets**: MUST be `0 <= offset < interval`
- **Bit masking formula**: `updateIndex & (interval - 1) == offset`

### Common Entity Patterns

**Citizen Looking for Job**:
```
Citizen Entity:
- Citizen
- HouseholdMember
- HasJobSeeker (enabled, references JobSeeker entity)

JobSeeker Entity:
- Owner (references Citizen)
- JobSeeker (contains education level)
- CurrentBuilding (references home)
- PathInformation (Pending → Finished)
```

**Citizen Going Shopping**:
```
Citizen Entity:
- Citizen
- ResourceBuyer (resource type, amount, payer)
- TripNeeded (Shopping purpose, target shop)
- PathInformation (pathfinding to shop)
```

**Commuter Household**:
```
Household Entity:
- Household (with Commuter flag)
- CurrentBuilding (outside connection)
- CommuterHousehold (origin = outside connection)
```

---

## See Also

- [Systems Guide](systems.md) - System types and update phases
- [Tool System Guide](tool.md) - Creating tool systems
