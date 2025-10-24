# 🎯 Unity Analytics Setup Guide

## 📊 **What's Being Tracked**

### **1. Player Rounds/Score Progression**
- **`session_started`** - When game starts
- **`level_started`** - When new level begins  
- **`hand_completed`** - Each hand with score and progression
- **`battle_completed`** - Level completion with final scores

### **2. Dice Usage Frequency**
- **`dice_used`** - Which dice are selected most often

### **3. Score Combination Frequency**
- **`score_combination`** - Frequency of "Three of a Kind", "Full House", etc.

## 🔧 **Setup Steps**

### **1. Unity Analytics is Already Installed**
✅ You have Unity Analytics 6.1.1 installed (I can see it in your Package Manager)

### **2. Configure Unity Services**
1. **Open Unity Services Dashboard**:
   - Go to [Unity Dashboard](https://dashboard.unity3d.com)
   - Sign in with your Unity account
   - Create a new project or select existing one

2. **Enable Analytics**:
   - In your Unity project, go to **Window > General > Services**
   - Sign in to Unity Services
   - Enable **Analytics** service

3. **Build and Deploy**:
   - Build your WebGL game
   - Deploy to GitHub Pages or your hosting platform
   - Analytics will start collecting data automatically

## 📈 **Viewing Your Data**

### **Unity Analytics Dashboard**
1. Go to [Unity Dashboard](https://dashboard.unity3d.com)
2. Select your project
3. Go to **Analytics** section
4. View **Custom Events** to see your game data

### **Key Reports to Check**
- **Events > Custom Events** - See all your game events
- **Events > hand_completed** - Player progression
- **Events > dice_used** - Most popular dice
- **Events > score_combination** - Combo frequency

## 🎮 **Testing the Analytics**

1. **Play the game** - Complete at least one hand
2. **Check Console** - You should see `[UnityAnalytics]` debug messages
3. **Check Unity Dashboard** - Events should appear within a few minutes

## 🔍 **Debug Messages to Look For**

In the Unity Console, you should see:
```
[UnityAnalytics] session_started
[UnityAnalytics] level_started: Level 1, Target: 300
[UnityAnalytics] dice_used: Golden Dice (Legendary) in hand 1
[UnityAnalytics] hand_completed: Hand 1, Score: 150, Total: 150, Combo: Three of a Kind
[UnityAnalytics] score_combination: Three of a Kind = 150 points in hand 1
```

## 🚀 **Advantages of Unity Analytics**

- ✅ **No external setup** - Works out of the box
- ✅ **Reliable** - Built into Unity
- ✅ **Real-time data** - See events immediately
- ✅ **Rich dashboard** - Better visualization than GA4
- ✅ **No measurement ID needed** - Automatic configuration

The analytics system is now ready! Just build and deploy your game, then check the Unity Analytics dashboard for your data.
