# Simplified Unity Analytics Event Schemas for Dice Roguelike

## Core Metrics to Track
1. **Player rounds/score progression** - How far players get and their scores
2. **Dice usage frequency** - Which dice are used most often
3. **Score combination frequency** - Which combinations players achieve

## How to Set Up Event Schemas

1. **Go to Unity Analytics Event Manager:**
   - In Unity Editor: `Window > Services > Analytics > Event Manager`
   - Or visit: https://dashboard.unity3d.com → Your Project → Analytics → Event Manager

2. **Create these 3 simple event schemas:**

## Event Schema Definitions

### 1. player_progression
**Description:** Tracks player progress and scores

**Parameters:**
- `total_score` (Integer) - Total score achieved
- `hands_completed` (Integer) - Number of hands completed
- `level_reached` (Integer) - Highest level reached

### 2. dice_usage
**Description:** Tracks which dice are used

**Parameters:**
- `dice_name` (String) - Name of the dice used

### 3. score_combination
**Description:** Tracks score combinations achieved

**Parameters:**
- `combo_name` (String) - Name of the combination (e.g., "Three of a Kind", "Full House")

## Steps to Create Each Event

1. Click "Create Event" in the Event Manager
2. Enter the event name (e.g., "player_progression")
3. Add the parameters with correct types (Integer, String)
4. Save the event schema
5. Repeat for all 3 events

## After Creating Schemas

Once you've created the 3 event schemas:
1. Play your game in Unity Editor
2. Check the Analytics Debug Panel - events should now show as "Valid Events"
3. Check the Unity Dashboard - events should appear in the "Valid Events" tab
4. Data will be available for analysis and reporting

## Expected Results

After setting up the schemas, you should see:
- ✅ Events marked as "Valid" in the Event Browser
- ✅ Simple, focused analytics data for your core metrics
- ✅ Easy to analyze player behavior patterns
