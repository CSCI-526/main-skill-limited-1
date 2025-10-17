# 🌐 WebGL Analytics Guide for Dice Roguelike

## 📋 Overview
This guide covers how to implement analytics for your Unity WebGL game hosted on GitHub Pages, focusing on collecting data from players who access your game through a link.

## 🎯 Why Analytics Matter for Your Game

### **Key Questions to Answer**
- Are players using the cooldown system strategically?
- Which dice are most/least popular?
- How far do players typically progress?
- Is the game difficulty appropriate?
- Are there any technical issues affecting gameplay?

## 🔧 Implementation Options

### **Option 1: Google Analytics 4 (Recommended)**

#### **Setup Steps**
1. Go to [Google Analytics](https://analytics.google.com)
2. Create a new account and property
3. Get your Measurement ID (GA4-XXXXXXXXX)
4. Add tracking code to your GitHub Pages

#### **Add to index.html**
```html
<!-- Google Analytics -->
<script async src="https://www.googletagmanager.com/gtag/js?id=GA_MEASUREMENT_ID"></script>
<script>
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());
  gtag('config', 'GA_MEASUREMENT_ID');
</script>
```

#### **Unity Implementation**
```csharp
public class WebAnalytics : MonoBehaviour
{
    public static void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Send to Google Analytics
        string jsonParams = parameters != null ? JsonUtility.ToJson(parameters) : "{}";
        Application.ExternalCall("gtag", "event", eventName, jsonParams);
        #else
        Debug.Log($"[Analytics] {eventName}: {JsonUtility.ToJson(parameters)}");
        #endif
    }
    
    public static void TrackDiceUsed(string diceName, int handNumber)
    {
        var parameters = new Dictionary<string, object>
        {
            {"dice_name", diceName},
            {"hand_number", handNumber}
        };
        TrackEvent("dice_used", parameters);
    }
    
    public static void TrackHandCompleted(int handNumber, int rollsUsed, int diceSubmitted)
    {
        var parameters = new Dictionary<string, object>
        {
            {"hand_number", handNumber},
            {"rolls_used", rollsUsed},
            {"dice_submitted", diceSubmitted}
        };
        TrackEvent("hand_completed", parameters);
    }
}
```

### **Option 2: Simple Server Endpoint**

#### **Server Setup (Node.js)**
```javascript
// server.js
const express = require('express');
const fs = require('fs');
const app = express();
app.use(express.json());

app.post('/api/analytics', (req, res) => {
    const timestamp = new Date().toISOString();
    const logEntry = `${timestamp}: ${JSON.stringify(req.body)}\n`;
    
    // Append to log file
    fs.appendFileSync('analytics.log', logEntry);
    
    console.log('Analytics received:', req.body);
    res.json({status: 'success'});
});

app.listen(process.env.PORT || 3000);
```

#### **Unity Implementation**
```csharp
public class SimpleAnalytics : MonoBehaviour
{
    private static string serverUrl = "https://your-server.herokuapp.com/api/analytics";
    
    public static void TrackEvent(string eventName, string data)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        var payload = new
        {
            event = eventName,
            data = data,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            userAgent = "WebGL"
        };
        
        Application.ExternalCall("fetch", serverUrl, new Dictionary<string, object>
        {
            {"method", "POST"},
            {"headers", new Dictionary<string, string> {{"Content-Type", "application/json"}}},
            {"body", JsonUtility.ToJson(payload)}
        });
        #else
        Debug.Log($"[Analytics] {eventName}: {data}");
        #endif
    }
}
```

### **Option 3: Firebase Analytics**

#### **Setup Steps**
1. Go to [Firebase Console](https://console.firebase.google.com)
2. Create new project
3. Add Web app to project
4. Get Firebase config

#### **Add to index.html**
```html
<!-- Firebase SDK -->
<script src="https://www.gstatic.com/firebasejs/9.0.0/firebase-app.js"></script>
<script src="https://www.gstatic.com/firebasejs/9.0.0/firebase-analytics.js"></script>
<script>
  const firebaseConfig = {
    // Your Firebase config
  };
  firebase.initializeApp(firebaseConfig);
  const analytics = firebase.analytics();
</script>
```

#### **Unity Implementation**
```csharp
public class FirebaseAnalytics : MonoBehaviour
{
    public static void LogEvent(string eventName, Dictionary<string, object> parameters)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalCall("analytics.logEvent", eventName, parameters);
        #else
        Debug.Log($"[Firebase] {eventName}: {JsonUtility.ToJson(parameters)}");
        #endif
    }
}
```

## 🎮 Integration with Your Game

### **Add to CooldownSystem.cs**
```csharp
public void CompleteHand(List<BaseDice> submittedDice = null)
{
    // ... existing code ...
    
    // Track analytics
    if (submittedDice != null && submittedDice.Count > 0)
    {
        foreach (var dice in submittedDice)
        {
            WebAnalytics.TrackDiceUsed(dice.diceName, currentHandCount);
        }
    }
    
    // ... rest of existing code ...
}
```

### **Add to BattleController.cs**
```csharp
void OnSubmitCombo()
{
    // ... existing code ...
    
    // Track hand completion
    WebAnalytics.TrackHandCompleted(currentHand + 1, _rollsUsed, submittedDice.Count);
    
    // ... rest of existing code ...
}
```

## 📊 Key Events to Track

### **Core Gameplay Events**
```csharp
// Dice selection
WebAnalytics.TrackEvent("dice_selected", new Dictionary<string, object>
{
    {"dice_name", dice.diceName},
    {"dice_tier", dice.tier.ToString()},
    {"dice_cost", dice.cost}
});

// Hand progression
WebAnalytics.TrackEvent("hand_started", new Dictionary<string, object>
{
    {"hand_number", handNumber},
    {"available_dice", availableDiceCount}
});

// Cooldown impact
WebAnalytics.TrackEvent("cooldown_applied", new Dictionary<string, object>
{
    {"submitted_dice_count", submittedCount},
    {"cooldown_dice", string.Join(",", submittedDice.Select(d => d.diceName))}
});

// Session end
WebAnalytics.TrackEvent("session_end", new Dictionary<string, object>
{
    {"total_hands", totalHands},
    {"total_dice_used", totalDiceUsed},
    {"session_duration", sessionTime}
});
```

## 📈 Data Analysis

### **Google Analytics Dashboard**
- **Real-time reports**: See current players
- **Event reports**: Track custom events
- **User flow**: Understand player journey
- **Geographic data**: Where players are from

### **Key Metrics to Monitor**
1. **Player Engagement**
   - Average session duration
   - Hands completed per session
   - Return player rate

2. **Game Balance**
   - Most/least used dice
   - Average rolls per hand
   - Hand completion rates

3. **Technical Issues**
   - Error rates
   - Performance metrics
   - Browser compatibility

## 🚀 Quick Start Recommendation

### **For Alpha Prototype: Use Google Analytics**

**Why?**
- Free and reliable
- No server maintenance
- Real-time data
- Easy to setup

**Steps:**
1. Create Google Analytics account (5 minutes)
2. Add tracking code to GitHub Pages (2 minutes)
3. Add Unity analytics calls (10 minutes)
4. Start collecting data immediately

**Expected Results:**
- Real-time player count
- Event tracking dashboard
- User behavior insights
- Geographic player distribution

## 📝 Sample Analytics Code

### **Complete Implementation**
```csharp
using System.Collections.Generic;
using UnityEngine;

public class GameAnalytics : MonoBehaviour
{
    private static GameAnalytics instance;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    
    public static void TrackDiceUsed(string diceName, int handNumber)
    {
        var parameters = new Dictionary<string, object>
        {
            {"dice_name", diceName},
            {"hand_number", handNumber},
            {"timestamp", System.DateTime.Now.ToString()}
        };
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalCall("gtag", "event", "dice_used", JsonUtility.ToJson(parameters));
        #else
        Debug.Log($"[Analytics] dice_used: {diceName} in hand {handNumber}");
        #endif
    }
    
    public static void TrackHandCompleted(int handNumber, int rollsUsed, int diceSubmitted)
    {
        var parameters = new Dictionary<string, object>
        {
            {"hand_number", handNumber},
            {"rolls_used", rollsUsed},
            {"dice_submitted", diceSubmitted},
            {"timestamp", System.DateTime.Now.ToString()}
        };
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalCall("gtag", "event", "hand_completed", JsonUtility.ToJson(parameters));
        #else
        Debug.Log($"[Analytics] hand_completed: hand {handNumber}, {rollsUsed} rolls, {diceSubmitted} dice");
        #endif
    }
    
    public static void TrackSessionEnd(int totalHands, int totalDiceUsed, float sessionTime)
    {
        var parameters = new Dictionary<string, object>
        {
            {"total_hands", totalHands},
            {"total_dice_used", totalDiceUsed},
            {"session_duration", sessionTime},
            {"timestamp", System.DateTime.Now.ToString()}
        };
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalCall("gtag", "event", "session_end", JsonUtility.ToJson(parameters));
        #else
        Debug.Log($"[Analytics] session_end: {totalHands} hands, {totalDiceUsed} dice, {sessionTime}s");
        #endif
    }
}
```

## 🎯 Next Steps

1. **Choose your analytics solution** (Google Analytics recommended)
2. **Implement the tracking code** in your Unity project
3. **Deploy to GitHub Pages** with analytics enabled
4. **Monitor the data** for insights
5. **Iterate based on findings** to improve your game

## 📚 Resources

- [Google Analytics Setup Guide](https://support.google.com/analytics/answer/9304153)
- [Unity WebGL Analytics](https://docs.unity3d.com/Manual/webgl-analytics.html)
- [Firebase Analytics Documentation](https://firebase.google.com/docs/analytics)
- [GitHub Pages Deployment](https://pages.github.com/)

---

**Remember**: Start simple with Google Analytics for your alpha prototype. You can always add more sophisticated tracking later as your game evolves!
