# 🎲 Relic Descriptions

Copy these descriptions into your Unity relic ScriptableObjects!

---

## 🟩 COMMON RELICS

### **Straight Edge**
```
Sharpen your sequences! Straights gain +15 base score and ×1.2 multiplier. But matching sets (Three/Four of a Kind) suffer a ×0.9 penalty. Choose your path wisely.
```

### **Pair Bond**
```
United we stand! Any pair grants +10 base score. Two Pair or Full House? Enjoy a ×1.15 multiplier! But rolling no pairs inflicts a ×0.95 penalty.
```

### **Momentum Gyro**
```
Patience pays off! Using all your rolls grants +10 base score and ×1.15 multiplier. Submitting early (1 roll or less) causes a ×0.9 penalty. Take your time!
```

---

## 🟪 RARE RELICS

### **Tight Purse**
```
Grants +1 hand budget, but demands full commitment. Leave budget unspent and suffer a ×0.95 penalty. Every coin counts!
```

### **Cooldown Radiator**
```
Experimental technology allows one cooling die to be selected, but adds +1 cooldown to your next hand. Use power now, pay later.
```

### **Filler Battery**
```
When using filler dice, gain +1 bonus reroll to compensate. However, your next hand loses -1 budget. Efficiency through sacrifice.
```

### **Loaded Coin**
```
Each 6 rolled grants +5% multiplier (max +25% total). But any 1s limit your multiplier to ×0.85. High risk, high reward!
```

### **Crown of Excess**
```
Spending your full hand budget rewards you with ×1.15 multiplier. Being thrifty (budget -2 or more unspent) causes ×0.95 penalty. Go big or go home!
```

### **Echo Prism**
```
Your highest die value resonates through the prism, adding its value again to your base score. A 6 becomes worth 12!
```

---

## 🟨 LEGENDARY RELICS

### **Collector's Seal**
```
The ultimate matching bonus! Three of a Kind: +15 base score and ×1.1 multiplier. Four or Five of a Kind: a massive +25 base score! Collect them all!
```

---

## 📋 Quick Reference

| Relic | Type | Best For | Watch Out |
|-------|------|----------|-----------|
| **Straight Edge** | Common | Straight builds | Avoid if going for sets |
| **Pair Bond** | Common | Pair-focused hands | Needs at least one pair |
| **Momentum Gyro** | Common | Patient players | Don't submit early! |
| **Tight Purse** | Rare | High-cost dice | Must spend fully |
| **Cooldown Radiator** | Rare | Emergency dice | Next hand penalty |
| **Filler Battery** | Rare | Filler situations | Next hand -1 budget |
| **Loaded Coin** | Rare | High rolls | Avoid 1s at all costs |
| **Crown of Excess** | Rare | Expensive builds | Must max out budget |
| **Echo Prism** | Rare | High-value dice | Always good! |
| **Collector's Seal** | Legendary | Set builds | Synergizes with sets |

---

## 🎯 Synergies

**Collector's Seal + Pair Bond**
- Full House triggers both! Massive score potential.

**Straight Edge + Crown of Excess**
- Use expensive dice for straights, spend full budget for double bonus!

**Echo Prism + Loaded Coin**
- Rolling 6s duplicates value AND boosts multiplier!

**Momentum Gyro + Filler Battery**
- Use all rolls even with fillers, max out both bonuses!

---

## ⚠️ Anti-Synergies

**Straight Edge + Collector's Seal**
- One buffs straights, one buffs sets. Pick a strategy!

**Tight Purse + Crown of Excess**
- Both demand full budget spending. Redundant but consistent.

**Pair Bond + Straight Edge**
- Full House has a pair, but if you go for sets Straight Edge penalizes you.

---

Copy the descriptions above into Unity:
1. Navigate to `Assets/Resources/Relics/`
2. Select each relic ScriptableObject
3. Paste the description into the **Description** field
4. Save!

Now your tooltips will show these descriptions! 🎉

