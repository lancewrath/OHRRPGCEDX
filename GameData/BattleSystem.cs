using System;
using System.Collections.Generic;
using System.Linq;

namespace OHRRPGCEDX.GameData
{
    /// <summary>
    /// Battle system for turn-based combat
    /// </summary>
    public class BattleSystem
    {
        private BattleState currentState;
        private List<HeroData> party;
        private List<EnemyData> enemies;
        private List<BattleSprite> battleSprites;
        private List<AttackQueue> attackQueue;
        private int currentTurnIndex;
        private BattlePhase currentPhase;
        
        // Battle configuration
        private bool isActiveTime;
        private float turnTimer;
        private float maxTurnTime;
        private int battleTicks;
        
        // Battle state
        private int actingEntity;
        private int nextHero;
        private int nextEnemy;
        private BattleMenuMode menuMode;
        private DeathMode deathMode;
        private TargettingState targettingState;
        private AttackState attackState;
        private VictoryState victoryState;
        private RewardsState rewardsState;
        
        // Events
        public event EventHandler<BattleStateChangedEventArgs> BattleStateChanged;
        public event EventHandler<TurnChangedEventArgs> TurnChanged;
        public event EventHandler<BattleEndedEventArgs> BattleEnded;
        public event EventHandler<AttackExecutedEventArgs> AttackExecuted;
        
        public BattleState CurrentState => currentState;
        public BattlePhase CurrentPhase => currentPhase;
        public bool IsBattleActive => currentState == BattleState.InProgress;
        public int CurrentTurnIndex => currentTurnIndex;
        public int BattleTicks => battleTicks;
        
        public BattleSystem()
        {
            currentState = BattleState.NotStarted;
            currentPhase = BattlePhase.None;
            party = new List<HeroData>();
            enemies = new List<EnemyData>();
            battleSprites = new List<BattleSprite>();
            attackQueue = new List<AttackQueue>();
            currentTurnIndex = 0;
            isActiveTime = false;
            turnTimer = 0.0f;
            maxTurnTime = 10.0f; // 10 seconds per turn
            battleTicks = 0;
            
            // Initialize battle state
            actingEntity = -1;
            nextHero = 0;
            nextEnemy = 0;
            menuMode = BattleMenuMode.HeroMenu;
            deathMode = DeathMode.Nobody;
            
            // Initialize targetting state
            targettingState = new TargettingState
            {
                Mode = TargetMode.None,
                Pointer = -1,
                HitDead = false,
                Mask = new bool[11],
                Selected = new bool[11],
                OptSpread = 0,
                Interactive = false,
                Roulette = false,
                ForceFirst = false,
                Attack = null,
                Hover = -1,
                MouseOptionalSpread = false,
                MustHoverValidTarget = false
            };
            
            // Initialize attack state
            attackState = new AttackState
            {
                ID = -1,
                WasID = -1,
                NonElemental = true,
                Elemental = new bool[8],
                HasConsumedCosts = false,
                HasSpawned = false
            };
            
            // Initialize victory state
            victoryState = new VictoryState
            {
                State = VictoryStateEnum.None,
                ShowLearn = false,
                LearnWho = -1,
                LearnList = -1,
                LearnSlot = -1,
                ItemName = "",
                FoundIndex = -1,
                GoldCaption = "",
                ExpCaption = "",
                ItemCaption = "",
                PluralItemCaption = "",
                ExpName = "",
                LevelUpCaption = "",
                LevelsUpCaption = "",
                LearnedCaption = "",
                DisplayTicks = 0
            };
            
            // Initialize rewards state
            rewardsState = new RewardsState
            {
                Gold = 0,
                Experience = 0,
                Items = new List<int>(),
                ItemCounts = new List<int>(),
                Found = new bool[10],
                DisplayTicks = 0
            };
        }
        
        /// <summary>
        /// Start a new battle
        /// </summary>
        public void StartBattle(List<HeroData> partyMembers, List<EnemyData> enemyGroup)
        {
            try
            {
                // Initialize battle
                party.Clear();
                enemies.Clear();
                battleSprites.Clear();
                attackQueue.Clear();
                
                // Add party members
                foreach (var hero in partyMembers)
                {
                    party.Add(hero);
                    var battleSprite = new BattleSprite
                    {
                        ID = hero.ID,
                        Name = hero.Name,
                        Type = EntityType.Hero,
                        Stats = new BattleStats
                        {
                            Current = new BattleStatsSingle
                            {
                                HP = hero.BaseStats.HP,
                                MP = hero.BaseStats.MP,
                                Strength = hero.BaseStats.Attack,
                                Accuracy = hero.BaseStats.Attack,
                                Defense = hero.BaseStats.Defense,
                                Dodge = hero.BaseStats.Speed,
                                Magic = hero.BaseStats.Magic,
                                Will = hero.BaseStats.MagicDef,
                                Speed = hero.BaseStats.Speed,
                                Counter = 0,
                                Focus = 0,
                                Hits = 1,
                                Poison = 0,
                                Regen = 0,
                                Stun = 0,
                                Mute = 0
                            },
                            Max = new BattleStatsSingle
                            {
                                HP = hero.BaseStats.HP,
                                MP = hero.BaseStats.MP,
                                Strength = hero.BaseStats.Attack,
                                Accuracy = hero.BaseStats.Attack,
                                Defense = hero.BaseStats.Defense,
                                Dodge = hero.BaseStats.Speed,
                                Magic = hero.BaseStats.Magic,
                                Will = hero.BaseStats.MagicDef,
                                Speed = hero.BaseStats.Speed,
                                Counter = 0,
                                Focus = 0,
                                Hits = 1,
                                Poison = 0,
                                Regen = 0,
                                Stun = 0,
                                Mute = 0
                            }
                        },
                        Level = hero.Level,
                        Ready = false,
                        ReadyMeter = 0,
                        Position = new XYPair(50 + battleSprites.Count * 60, 150),
                        BasePosition = new XYPair(50 + battleSprites.Count * 60, 150),
                        Visible = true,
                        Hidden = false,
                        Flipped = false,
                        UnderPlayerControl = true,
                        TurncoatAttacker = false,
                        DefectorTarget = false,
                        Dissolve = 0,
                        DissolveAppear = 0,
                        Fleeing = false,
                        FlinchAnim = 0,
                        AttackSucceeded = false,
                        Walk = 0,
                        AnimPattern = 0,
                        AnimIndex = 0,
                        DeathType = 0,
                        DeathTime = 0,
                        AppearType = -1,
                        AppearTime = 0,
                        DeathSFX = 0,
                        RevengeHarm = 0,
                        ThankVengeCure = 0,
                        RepeatHarm = 0,
                        CursorPos = new XYPair(0, 0),
                        Hand = new XYPair[2] { new XYPair(0, 0), new XYPair(0, 0) },
                        InitiativeOrder = 0,
                        NoAttackThisTurn = 0,
                        ActiveTurnNum = 0,
                        PoisonRepeat = 0,
                        RegenRepeat = 0,
                        Attack = -1,
                        Revenge = -1,
                        ThankVenge = -1,
                        CounterTarget = -1,
                        ElementalDamage = new float[8],
                        ConsumeLMP = -1,
                        ConsumeItem = -1,
                        IsBoss = false,
                        Unescapable = false,
                        DieWithoutBoss = false,
                        FleeInsteadOfDie = false,
                        EnemyUntargetable = false,
                        HeroUntargetable = false,
                        DeathUnneeded = false,
                        NeverFlinch = false,
                        IgnoreForAlone = false,
                        GiveRewardsEvenIfAlive = false,
                        Bequesting = false,
                        SelfBequesting = false
                    };
                    battleSprites.Add(battleSprite);
                }
                
                // Add enemies
                foreach (var enemy in enemyGroup)
                {
                    enemies.Add(enemy);
                    var battleSprite = new BattleSprite
                    {
                        ID = enemy.ID,
                        Name = enemy.Name,
                        Type = EntityType.Enemy,
                        Stats = new BattleStats
                        {
                            Current = new BattleStatsSingle
                            {
                                HP = enemy.BaseStats.HP,
                                MP = enemy.BaseStats.MP,
                                Strength = enemy.BaseStats.Attack,
                                Accuracy = enemy.BaseStats.Attack,
                                Defense = enemy.BaseStats.Defense,
                                Dodge = enemy.BaseStats.Speed,
                                Magic = enemy.BaseStats.Magic,
                                Will = enemy.BaseStats.MagicDef,
                                Speed = enemy.BaseStats.Speed,
                                Counter = 0,
                                Focus = 0,
                                Hits = 1,
                                Poison = 0,
                                Regen = 0,
                                Stun = 0,
                                Mute = 0
                            },
                            Max = new BattleStatsSingle
                            {
                                HP = enemy.BaseStats.HP,
                                MP = enemy.BaseStats.MP,
                                Strength = enemy.BaseStats.Attack,
                                Accuracy = enemy.BaseStats.Attack,
                                Defense = enemy.BaseStats.Defense,
                                Dodge = enemy.BaseStats.Speed,
                                Magic = enemy.BaseStats.Magic,
                                Will = enemy.BaseStats.MagicDef,
                                Speed = enemy.BaseStats.Speed,
                                Counter = 0,
                                Focus = 0,
                                Hits = 1,
                                Poison = 0,
                                Regen = 0,
                                Stun = 0,
                                Mute = 0
                            }
                        },
                        Level = 1, // Enemies don't have levels, use default
                        Ready = false,
                        ReadyMeter = 0,
                        Position = new XYPair(400 + battleSprites.Count * 60, 150),
                        BasePosition = new XYPair(400 + battleSprites.Count * 60, 150),
                        Visible = true,
                        Hidden = false,
                        Flipped = false,
                        UnderPlayerControl = false,
                        TurncoatAttacker = false,
                        DefectorTarget = false,
                        Dissolve = 0,
                        DissolveAppear = 0,
                        Fleeing = false,
                        FlinchAnim = 0,
                        AttackSucceeded = false,
                        Walk = 0,
                        AnimPattern = 0,
                        AnimIndex = 0,
                        DeathType = 0,
                        DeathTime = 0,
                        AppearType = -1,
                        AppearTime = 0,
                        DeathSFX = 0,
                        RevengeHarm = 0,
                        ThankVengeCure = 0,
                        RepeatHarm = 0,
                        CursorPos = new XYPair(0, 0),
                        Hand = new XYPair[2] { new XYPair(0, 0), new XYPair(0, 0) },
                        InitiativeOrder = 0,
                        NoAttackThisTurn = 0,
                        ActiveTurnNum = 0,
                        PoisonRepeat = 0,
                        RegenRepeat = 0,
                        Attack = -1,
                        Revenge = -1,
                        ThankVenge = -1,
                        CounterTarget = -1,
                        ElementalDamage = new float[8],
                        ConsumeLMP = -1,
                        ConsumeItem = -1,
                        IsBoss = enemy.Behavior == EnemyBehavior.Aggressive,
                        Unescapable = false,
                        DieWithoutBoss = false,
                        FleeInsteadOfDie = false,
                        EnemyUntargetable = false,
                        HeroUntargetable = false,
                        DeathUnneeded = false,
                        NeverFlinch = false,
                        IgnoreForAlone = false,
                        GiveRewardsEvenIfAlive = false,
                        Bequesting = false,
                        SelfBequesting = false
                    };
                    battleSprites.Add(battleSprite);
                }
                
                // Initialize battle state
                currentState = BattleState.InProgress;
                currentPhase = BattlePhase.PlayerTurn;
                currentTurnIndex = 0;
                turnTimer = 0.0f;
                battleTicks = 0;
                actingEntity = -1;
                nextHero = 0;
                nextEnemy = 0;
                menuMode = BattleMenuMode.HeroMenu;
                deathMode = DeathMode.Nobody;
                
                // Initialize targetting state
                targettingState = new TargettingState
                {
                    Mode = TargetMode.None,
                    Pointer = -1,
                    HitDead = false,
                    Mask = new bool[11],
                    Selected = new bool[11],
                    OptSpread = 0,
                    Interactive = false,
                    Roulette = false,
                    ForceFirst = false,
                    Attack = null,
                    Hover = -1,
                    MouseOptionalSpread = false,
                    MustHoverValidTarget = false
                };
                
                // Initialize attack state
                attackState = new AttackState
                {
                    ID = -1,
                    WasID = -1,
                    NonElemental = true,
                    Elemental = new bool[8],
                    HasConsumedCosts = false,
                    HasSpawned = false
                };
                
                // Initialize victory state
                victoryState = new VictoryState
                {
                    State = VictoryStateEnum.None,
                    ShowLearn = false,
                    LearnWho = -1,
                    LearnList = -1,
                    LearnSlot = -1,
                    ItemName = "",
                    FoundIndex = -1,
                    GoldCaption = "",
                    ExpCaption = "",
                    ItemCaption = "",
                    PluralItemCaption = "",
                    ExpName = "",
                    LevelUpCaption = "",
                    LevelsUpCaption = "",
                    LearnedCaption = "",
                    DisplayTicks = 0
                };
                
                // Initialize rewards state
                rewardsState = new RewardsState
                {
                    Gold = 0,
                    Experience = 0,
                    Items = new List<int>(),
                    ItemCounts = new List<int>(),
                    Found = new bool[10],
                    DisplayTicks = 0
                };
                
                // Trigger events
                BattleStateChanged?.Invoke(this, new BattleStateChangedEventArgs(currentState));
                TurnChanged?.Invoke(this, new TurnChangedEventArgs(currentTurnIndex, GetCurrentEntity()));
                
                Console.WriteLine($"Battle started! {party.Count} heroes vs {enemies.Count} enemies");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start battle: {ex.Message}");
                currentState = BattleState.Error;
            }
        }
        
        /// <summary>
        /// Update battle logic
        /// </summary>
        public void Update(float deltaTime)
        {
            if (currentState != BattleState.InProgress) return;
            
            battleTicks++;
            
            switch (currentPhase)
            {
                case BattlePhase.PlayerTurn:
                    UpdatePlayerTurn(deltaTime);
                    break;
                    
                case BattlePhase.EnemyTurn:
                    UpdateEnemyTurn(deltaTime);
                    break;
                    
                case BattlePhase.BattleRewards:
                    UpdateBattleRewards(deltaTime);
                    break;
            }
            
            // Update attack queue
            UpdateAttackQueue();
            
            // Update ready meters for active time battles
            if (isActiveTime)
            {
                UpdateReadyMeters();
            }
        }
        
        /// <summary>
        /// Update player turn logic
        /// </summary>
        private void UpdatePlayerTurn(float deltaTime)
        {
            if (isActiveTime)
            {
                turnTimer += deltaTime;
                if (turnTimer >= maxTurnTime)
                {
                    // Auto-select basic attack if time runs out
                    var currentEntity = GetCurrentEntity();
                    if (currentEntity != null && currentEntity.Type == EntityType.Hero)
                    {
                        ExecuteAction(currentEntity, BattleAction.BasicAttack, GetRandomEnemy());
                    }
                }
            }
        }
        
        /// <summary>
        /// Update enemy turn logic
        /// </summary>
        private void UpdateEnemyTurn(float deltaTime)
        {
            var currentEntity = GetCurrentEntity();
            if (currentEntity != null && currentEntity.Type == EntityType.Enemy)
            {
                // Simple AI: random action
                var actions = new[] { BattleAction.BasicAttack, BattleAction.SpecialAttack, BattleAction.Defend };
                var randomAction = actions[new Random().Next(actions.Length)];
                
                // Target random hero
                var target = GetRandomHero();
                
                ExecuteAction(currentEntity, randomAction, target);
            }
        }
        
        /// <summary>
        /// Update battle rewards phase
        /// </summary>
        private void UpdateBattleRewards(float deltaTime)
        {
            // TODO: Implement battle rewards display
            EndBattle(BattleResult.Victory);
        }
        
        /// <summary>
        /// Update attack queue
        /// </summary>
        private void UpdateAttackQueue()
        {
            for (int i = attackQueue.Count - 1; i >= 0; i--)
            {
                var attack = attackQueue[i];
                if (attack.Used) continue;
                
                if (attack.Delay <= 0)
                {
                    // Execute attack
                    ExecuteQueuedAttack(attack);
                    attackQueue.RemoveAt(i);
                }
                else
                {
                    attack.Delay--;
                }
            }
        }
        
        /// <summary>
        /// Update ready meters for active time battles
        /// </summary>
        private void UpdateReadyMeters()
        {
            foreach (var sprite in battleSprites)
            {
                if (sprite.Status == EntityStatus.Dead) continue;
                
                sprite.ReadyMeter += sprite.Stats.Current.Speed;
                if (sprite.ReadyMeter >= 1000)
                {
                    sprite.Ready = true;
                    sprite.ReadyMeter = 1000;
                }
            }
        }
        
        /// <summary>
        /// Execute a battle action
        /// </summary>
        public void ExecuteAction(BattleSprite actor, BattleAction action, BattleSprite target)
        {
            if (actor == null || target == null) return;
            
            try
            {
                int damage = 0;
                string message = "";
                
                switch (action)
                {
                    case BattleAction.BasicAttack:
                        damage = CalculateDamage(actor, target, false);
                        target.Stats.Current.HP = Math.Max(0, target.Stats.Current.HP - damage);
                        message = $"{actor.Name} attacks {target.Name} for {damage} damage!";
                        break;
                        
                    case BattleAction.SpecialAttack:
                        if (actor.Stats.Current.MP >= 10)
                        {
                            damage = CalculateDamage(actor, target, true);
                            target.Stats.Current.HP = Math.Max(0, target.Stats.Current.HP - damage);
                            actor.Stats.Current.MP = Math.Max(0, actor.Stats.Current.MP - 10);
                            message = $"{actor.Name} uses special attack on {target.Name} for {damage} damage!";
                        }
                        else
                        {
                            message = $"{actor.Name} doesn't have enough MP!";
                        }
                        break;
                        
                    case BattleAction.Defend:
                        actor.Stats.Current.Defense = (int)(actor.Stats.Current.Defense * 1.5f); // Temporary defense boost
                        message = $"{actor.Name} takes a defensive stance!";
                        break;
                        
                    case BattleAction.UseItem:
                        // TODO: Implement item usage
                        message = $"{actor.Name} uses an item!";
                        break;
                        
                    case BattleAction.Run:
                        if (CanRun())
                        {
                            EndBattle(BattleResult.Run);
                            return;
                        }
                        else
                        {
                            message = "Cannot run from this battle!";
                        }
                        break;
                }
                
                Console.WriteLine(message);
                
                // Check if target is defeated
                if (target.Stats.Current.HP <= 0)
                {
                    target.Stats.Current.HP = 0;
                    target.Status = EntityStatus.Dead;
                    Console.WriteLine($"{target.Name} is defeated!");
                    
                    // Check battle end conditions
                    CheckBattleEndConditions();
                }
                
                // Move to next turn if not game over
                if (currentState == BattleState.InProgress)
                {
                    NextTurn();
                }
                
                // Trigger attack executed event
                AttackExecuted?.Invoke(this, new AttackExecutedEventArgs(actor, target, action, damage));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute action: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Execute a queued attack
        /// </summary>
        private void ExecuteQueuedAttack(AttackQueue attack)
        {
            var attacker = battleSprites.FirstOrDefault(s => s.ID == attack.Attacker);
            if (attacker == null) return;
            
            var targets = attack.Targets.Where(t => t >= 0).Select(t => battleSprites[t]).ToList();
            if (targets.Count == 0) return;
            
            // Execute attack on all targets
            foreach (var target in targets)
            {
                if (target.Status == EntityStatus.Dead) continue;
                
                var damage = CalculateDamage(attacker, target, false);
                target.Stats.Current.HP = Math.Max(0, target.Stats.Current.HP - damage);
                
                Console.WriteLine($"{attacker.Name} attacks {target.Name} for {damage} damage!");
                
                if (target.Stats.Current.HP <= 0)
                {
                    target.Status = EntityStatus.Dead;
                    Console.WriteLine($"{target.Name} is defeated!");
                }
            }
            
            // Check battle end conditions
            CheckBattleEndConditions();
        }
        
        /// <summary>
        /// Calculate damage for an attack
        /// </summary>
        private int CalculateDamage(BattleSprite attacker, BattleSprite defender, bool isSpecial)
        {
            float baseDamage = attacker.Stats.Current.Strength;
            if (isSpecial) baseDamage *= 1.5f;
            
            float defense = defender.Stats.Current.Defense;
            float damage = baseDamage - defense;
            
            // Add some randomness
            var random = new Random();
            float variance = 0.2f; // 20% variance
            damage *= 1.0f + (float)(random.NextDouble() * variance * 2 - variance);
            
            return Math.Max(1, (int)damage); // Minimum 1 damage
        }
        
        /// <summary>
        /// Check if the party can run from battle
        /// </summary>
        private bool CanRun()
        {
            // Check if any enemies are unescapable
            if (battleSprites.Any(s => s.Type == EntityType.Enemy && s.Unescapable))
                return false;
                
            // Check if any heroes are unescapable
            if (battleSprites.Any(s => s.Type == EntityType.Hero && s.Unescapable))
                return false;
                
            return true;
        }
        
        /// <summary>
        /// Move to the next turn
        /// </summary>
        private void NextTurn()
        {
            currentTurnIndex = (currentTurnIndex + 1) % battleSprites.Count;
            turnTimer = 0.0f;
            
            // Determine phase based on current entity
            var currentEntity = GetCurrentEntity();
            if (currentEntity != null)
            {
                currentPhase = currentEntity.Type == EntityType.Hero ? BattlePhase.PlayerTurn : BattlePhase.EnemyTurn;
                
                // Trigger turn change event
                TurnChanged?.Invoke(this, new TurnChangedEventArgs(currentTurnIndex, currentEntity));
                
                Console.WriteLine($"Turn {currentTurnIndex + 1}: {currentEntity.Name}'s turn");
            }
        }
        
        /// <summary>
        /// Check if battle should end
        /// </summary>
        private void CheckBattleEndConditions()
        {
            var aliveHeroes = battleSprites.Count(e => e.Type == EntityType.Hero && e.Status != EntityStatus.Dead);
            var aliveEnemies = battleSprites.Count(e => e.Type == EntityType.Enemy && e.Status != EntityStatus.Dead);
            
            if (aliveHeroes == 0)
            {
                EndBattle(BattleResult.Defeat);
            }
            else if (aliveEnemies == 0)
            {
                EndBattle(BattleResult.Victory);
            }
        }
        
        /// <summary>
        /// End the battle
        /// </summary>
        private void EndBattle(BattleResult result)
        {
            currentState = BattleState.Finished;
            currentPhase = BattlePhase.None;
            
            Console.WriteLine($"Battle ended! Result: {result}");
            
            // Trigger battle ended event
            BattleEnded?.Invoke(this, new BattleEndedEventArgs(result));
        }
        
        /// <summary>
        /// Get a random hero as target
        /// </summary>
        private BattleSprite GetRandomHero()
        {
            var heroes = battleSprites.Where(e => e.Type == EntityType.Hero && e.Status != EntityStatus.Dead).ToList();
            if (heroes.Count == 0) return null;
            
            var random = new Random();
            return heroes[random.Next(heroes.Count)];
        }
        
        /// <summary>
        /// Get a random enemy as target
        /// </summary>
        private BattleSprite GetRandomEnemy()
        {
            var enemies = battleSprites.Where(e => e.Type == EntityType.Enemy && e.Status != EntityStatus.Dead).ToList();
            if (enemies.Count == 0) return null;
            
            var random = new Random();
            return enemies[random.Next(enemies.Count)];
        }
        
        /// <summary>
        /// Get current battle entity
        /// </summary>
        public BattleSprite GetCurrentEntity()
        {
            if (currentTurnIndex >= 0 && currentTurnIndex < battleSprites.Count)
            {
                return battleSprites[currentTurnIndex];
            }
            return null;
        }
        
        /// <summary>
        /// Get all battle entities
        /// </summary>
        public List<BattleSprite> GetBattleEntities()
        {
            return new List<BattleSprite>(battleSprites);
        }
        
        /// <summary>
        /// Set battle mode (active time vs turn-based)
        /// </summary>
        public void SetBattleMode(bool activeTime)
        {
            isActiveTime = activeTime;
            if (!activeTime)
            {
                turnTimer = 0.0f; // Reset timer for turn-based mode
            }
        }
        
        /// <summary>
        /// Get remaining turn time
        /// </summary>
        public float GetRemainingTurnTime()
        {
            return Math.Max(0, maxTurnTime - turnTimer);
        }
        
        /// <summary>
        /// Queue an attack for later execution
        /// </summary>
        public void QueueAttack(int attackerID, int attackID, List<int> targetIDs, bool blocking = false, int delay = 0)
        {
            var attack = new AttackQueue
            {
                Used = true,
                Attack = attackID,
                Attacker = attackerID,
                Targets = targetIDs.ToArray(),
                Blocking = blocking,
                Delay = delay,
                TurnDelay = 0,
                DontRetarget = false
            };
            
            attackQueue.Add(attack);
        }
        
        /// <summary>
        /// Get battle statistics
        /// </summary>
        public BattleStatistics GetBattleStatistics()
        {
            return new BattleStatistics
            {
                TotalTurns = battleTicks,
                HeroesAlive = battleSprites.Count(s => s.Type == EntityType.Hero && s.Status != EntityStatus.Dead),
                EnemiesAlive = battleSprites.Count(s => s.Type == EntityType.Enemy && s.Status != EntityStatus.Dead),
                TotalDamageDealt = 0, // TODO: Track damage
                TotalDamageReceived = 0, // TODO: Track damage
                AttacksExecuted = 0, // TODO: Track attacks
                ItemsUsed = 0, // TODO: Track items
                SpellsCast = 0 // TODO: Track spells
            };
        }
    }
    
    /// <summary>
    /// Battle states
    /// </summary>
    public enum BattleState
    {
        NotStarted,
        InProgress,
        Paused,
        Finished,
        Error
    }
    
    /// <summary>
    /// Battle phases
    /// </summary>
    public enum BattlePhase
    {
        None,
        PlayerTurn,
        EnemyTurn,
        BattleRewards
    }
    
    /// <summary>
    /// Battle actions
    /// </summary>
    public enum BattleAction
    {
        BasicAttack,
        SpecialAttack,
        Defend,
        UseItem,
        Run
    }
    
    /// <summary>
    /// Battle results
    /// </summary>
    public enum BattleResult
    {
        Victory,
        Defeat,
        Run
    }
    
    /// <summary>
    /// Entity types in battle
    /// </summary>
    public enum EntityType
    {
        Hero,
        Enemy
    }
    
    /// <summary>
    /// Entity status
    /// </summary>
    public enum EntityStatus
    {
        Normal,
        Poisoned,
        Stunned,
        Dead
    }
    
    /// <summary>
    /// Battle menu modes
    /// </summary>
    public enum BattleMenuMode
    {
        HeroMenu,
        SpellMenu,
        ItemMenu
    }
    
    /// <summary>
    /// Death modes
    /// </summary>
    public enum DeathMode
    {
        Nobody,
        Enemies,
        Heroes
    }
    
    /// <summary>
    /// Target modes
    /// </summary>
    public enum TargetMode
    {
        None,
        Setup,
        Manual,
        Auto
    }
    
    /// <summary>
    /// Victory state enum
    /// </summary>
    public enum VictoryStateEnum
    {
        None,
        GoldExp,
        LevelUp,
        Spells,
        Items,
        ExitDelay = -1,
        Exit = -2
    }
    
    /// <summary>
    /// Battle entity for combat
    /// </summary>
    public class BattleSprite
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public EntityType Type { get; set; }
        public BattleStats Stats { get; set; }
        public int Level { get; set; }
        public bool Ready { get; set; }
        public int ReadyMeter { get; set; }
        public XYPair Position { get; set; }
        public XYPair BasePosition { get; set; }
        public int Direction { get; set; }
        public bool Visible { get; set; }
        public bool Hidden { get; set; }
        public bool Flipped { get; set; }
        public bool UnderPlayerControl { get; set; }
        public bool TurncoatAttacker { get; set; }
        public bool DefectorTarget { get; set; }
        public int Dissolve { get; set; }
        public int DissolveAppear { get; set; }
        public bool Fleeing { get; set; }
        public int FlinchAnim { get; set; }
        public bool AttackSucceeded { get; set; }
        public int Walk { get; set; }
        public int AnimPattern { get; set; }
        public int AnimIndex { get; set; }
        public int DeathType { get; set; }
        public int DeathTime { get; set; }
        public int AppearType { get; set; }
        public int AppearTime { get; set; }
        public int DeathSFX { get; set; }
        public int RevengeHarm { get; set; }
        public int ThankVengeCure { get; set; }
        public int RepeatHarm { get; set; }
        public XYPair CursorPos { get; set; }
        public XYPair[] Hand { get; set; }
        public int InitiativeOrder { get; set; }
        public int NoAttackThisTurn { get; set; }
        public int ActiveTurnNum { get; set; }
        public int PoisonRepeat { get; set; }
        public int RegenRepeat { get; set; }
        public int Attack { get; set; }
        public int Revenge { get; set; }
        public int ThankVenge { get; set; }
        public int CounterTarget { get; set; }
        public float[] ElementalDamage { get; set; }
        public int ConsumeLMP { get; set; }
        public int ConsumeItem { get; set; }
        public bool IsBoss { get; set; }
        public bool Unescapable { get; set; }
        public bool DieWithoutBoss { get; set; }
        public bool FleeInsteadOfDie { get; set; }
        public bool EnemyUntargetable { get; set; }
        public bool HeroUntargetable { get; set; }
        public bool DeathUnneeded { get; set; }
        public bool NeverFlinch { get; set; }
        public bool IgnoreForAlone { get; set; }
        public bool GiveRewardsEvenIfAlive { get; set; }
        public bool Bequesting { get; set; }
        public bool SelfBequesting { get; set; }
        public EntityStatus Status { get; set; } = EntityStatus.Normal;
        
        public float HPPercentage => Stats.Max.HP > 0 ? (float)Stats.Current.HP / Stats.Max.HP : 0;
        public float MPPercentage => Stats.Max.MP > 0 ? (float)Stats.Current.MP / Stats.Max.MP : 0;
        public bool IsAlive => Status != EntityStatus.Dead && Stats.Current.HP > 0;
    }
    
    /// <summary>
    /// Battle statistics
    /// </summary>
    public class BattleStats
    {
        public BattleStatsSingle Current { get; set; }
        public BattleStatsSingle Max { get; set; }
    }
    
    /// <summary>
    /// Single battle stats
    /// </summary>
    public class BattleStatsSingle
    {
        public int HP { get; set; }
        public int MP { get; set; }
        public int Strength { get; set; }
        public int Accuracy { get; set; }
        public int Defense { get; set; }
        public int Dodge { get; set; }
        public int Magic { get; set; }
        public int Will { get; set; }
        public int Speed { get; set; }
        public int Counter { get; set; }
        public int Focus { get; set; }
        public int Hits { get; set; }
        public int Poison { get; set; }
        public int Regen { get; set; }
        public int Stun { get; set; }
        public int Mute { get; set; }
    }
    
    /// <summary>
    /// Targetting state
    /// </summary>
    public class TargettingState
    {
        public TargetMode Mode { get; set; }
        public int Pointer { get; set; }
        public bool HitDead { get; set; }
        public bool[] Mask { get; set; } = new bool[11];
        public bool[] Selected { get; set; } = new bool[11];
        public int OptSpread { get; set; }
        public bool Interactive { get; set; }
        public bool Roulette { get; set; }
        public bool ForceFirst { get; set; }
        public AttackData Attack { get; set; }
        public int Hover { get; set; }
        public bool MouseOptionalSpread { get; set; }
        public bool MustHoverValidTarget { get; set; }
    }
    
    /// <summary>
    /// Attack state
    /// </summary>
    public class AttackState
    {
        public int ID { get; set; }
        public int WasID { get; set; }
        public bool NonElemental { get; set; }
        public bool[] Elemental { get; set; } = new bool[8];
        public bool HasConsumedCosts { get; set; }
        public bool HasSpawned { get; set; }
    }
    
    /// <summary>
    /// Victory state
    /// </summary>
    public class VictoryState
    {
        public VictoryStateEnum State { get; set; }
        public bool ShowLearn { get; set; }
        public int LearnWho { get; set; }
        public int LearnList { get; set; }
        public int LearnSlot { get; set; }
        public string ItemName { get; set; }
        public int FoundIndex { get; set; }
        public string GoldCaption { get; set; }
        public string ExpCaption { get; set; }
        public string ItemCaption { get; set; }
        public string PluralItemCaption { get; set; }
        public string ExpName { get; set; }
        public string LevelUpCaption { get; set; }
        public string LevelsUpCaption { get; set; }
        public string LearnedCaption { get; set; }
        public int DisplayTicks { get; set; }
    }
    
    /// <summary>
    /// Rewards state
    /// </summary>
    public class RewardsState
    {
        public int Gold { get; set; }
        public int Experience { get; set; }
        public List<int> Items { get; set; } = new List<int>();
        public List<int> ItemCounts { get; set; } = new List<int>();
        public bool[] Found { get; set; } = new bool[10];
        public int DisplayTicks { get; set; }
    }
    
    /// <summary>
    /// Attack queue
    /// </summary>
    public class AttackQueue
    {
        public bool Used { get; set; }
        public int Attack { get; set; }
        public int Attacker { get; set; }
        public int[] Targets { get; set; } = new int[11];
        public bool Blocking { get; set; }
        public int Delay { get; set; }
        public int TurnDelay { get; set; }
        public bool DontRetarget { get; set; }
    }
    
    /// <summary>
    /// Battle statistics
    /// </summary>
    public class BattleStatistics
    {
        public int TotalTurns { get; set; }
        public int HeroesAlive { get; set; }
        public int EnemiesAlive { get; set; }
        public int TotalDamageDealt { get; set; }
        public int TotalDamageReceived { get; set; }
        public int AttacksExecuted { get; set; }
        public int ItemsUsed { get; set; }
        public int SpellsCast { get; set; }
    }
    
    /// <summary>
    /// Battle state changed event arguments
    /// </summary>
    public class BattleStateChangedEventArgs : EventArgs
    {
        public BattleState NewState { get; }
        
        public BattleStateChangedEventArgs(BattleState newState)
        {
            NewState = newState;
        }
    }
    
    /// <summary>
    /// Turn changed event arguments
    /// </summary>
    public class TurnChangedEventArgs : EventArgs
    {
        public int TurnIndex { get; }
        public BattleSprite CurrentEntity { get; }
        
        public TurnChangedEventArgs(int turnIndex, BattleSprite currentEntity)
        {
            TurnIndex = turnIndex;
            CurrentEntity = currentEntity;
        }
    }
    
    /// <summary>
    /// Battle ended event arguments
    /// </summary>
    public class BattleEndedEventArgs : EventArgs
    {
        public BattleResult Result { get; }
        
        public BattleEndedEventArgs(BattleResult result)
        {
            Result = result;
        }
    }
    
    /// <summary>
    /// Attack executed event arguments
    /// </summary>
    public class AttackExecutedEventArgs : EventArgs
    {
        public BattleSprite Attacker { get; }
        public BattleSprite Target { get; }
        public BattleAction Action { get; }
        public int Damage { get; }
        
        public AttackExecutedEventArgs(BattleSprite attacker, BattleSprite target, BattleAction action, int damage)
        {
            Attacker = attacker;
            Target = target;
            Action = action;
            Damage = damage;
        }
    }
}
