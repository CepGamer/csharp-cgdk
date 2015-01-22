using System;
using System.Collections.Generic;
using Com.CodeGame.CodeHockey2014.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeHockey2014.DevKit.CSharpCgdk {
    public struct Point
    {
        public double X, Y;
        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public override string ToString()
        {
            return (String.Format("{0}, {1}", this.X, this.Y));//base.ToString();
        }
    }

    public enum Side
    {
        TOP,
        BOTTOM,
        LEFT,
        RIGHT
    }

    public enum Points
    {
        TOPLEFT,
        TOPRIGHT,
        BOTTOMLEFT,
        BOTTOMRIGHT
    }
    
    public struct Constants
    {
        public double HitPointRadius;
        public double NearNetStrike;

        public Constants(double hitPointRadius, double nearNetStrike)
        {
            this.HitPointRadius = hitPointRadius;
            this.NearNetStrike = nearNetStrike;
        }
    }

    public enum PuckOwner
    {
        ENEMY,
        PLAYER,
        NOBODY,
        HOCKEYIST
    }


    public sealed class MyStrategy : IStrategy
    {
        private bool initialized;
        public Hockeyist selfN;
        public World worldN;
        public Game gameN;
        public Move moveN;

        private IHockeyistStrategy strat;
        // save
        private IHockeyistStrategy mSavedStrat;

        private bool mReinitiated;

        public MyStrategy()
        {
            initialized = false;
            mReinitiated = false;
        }

        private void init(Hockeyist self, Game game, World world, double hitPointRadius, double nearNetStrike)
        {
            initialized = true;

            IHockeyistStrategy.Role role = IHockeyistStrategy.Role.ATTACK;
            role = isDefenderman(self, world, game) ? IHockeyistStrategy.Role.DEFENCE
                                                      : IHockeyistStrategy.Role.ATTACK;

            switch (role)
            {
                case IHockeyistStrategy.Role.DEFENCE:
                    strat = new Defenceman(game, hitPointRadius, nearNetStrike);
                    mSavedStrat = new Attacker(game, hitPointRadius, nearNetStrike);
                    break;
                case IHockeyistStrategy.Role.ATTACK:
                    strat = new Attacker(game, hitPointRadius, nearNetStrike);
                    mSavedStrat = new Attacker(game, hitPointRadius, nearNetStrike);
                    break;
                case IHockeyistStrategy.Role.UNIVERSAL:
                    break;
                default:
                    break;
            }
        }

        public void Move(Hockeyist self, World world, Game game, Move move) {
            if (!initialized)
                init(self, game, world, 80D, 300D);

            selfN = self;
            worldN = world;
            gameN = game;
            moveN = move;

            onlineChangeStrategy(self, world, game, 80D, 300D);

            strat.execute(this, self, world, game, move);
        }
         
        private bool isDefenderman(Hockeyist self, World world, Game game)
        {
            var minDist = game.WorldWidth;
            var defender = self;
            foreach (var man in world.Hockeyists)
            {
                if (Math.Abs(man.X - world.GetMyPlayer().NetLeft) < minDist && man.Type != HockeyistType.Goalie)
                {
                    defender = man;
                    minDist = Math.Abs(man.X - world.GetMyPlayer().NetLeft);
                }
            }
            return self.Id == defender.Id;
        }

        private void onlineChangeStrategy(Hockeyist self,  World world, Game game, double hitPointRadius, double nearNetStrike)
        {
            var owner = strat.havePuck(self, world);

            if (strat.getRole() == IHockeyistStrategy.Role.DEFENCE    // man is defender
                && canDefenderPlayAttacker(self, world, owner))                    
            {
                var temp = strat;
                strat = mSavedStrat;
                mSavedStrat = temp;
            } 
            else if (mSavedStrat.getRole() == IHockeyistStrategy.Role.DEFENCE //man is temporary attacker
                && owner != PuckOwner.HOCKEYIST)  // not anymore
            {
                var temp = strat;
                strat = mSavedStrat;
                mSavedStrat = temp;
            }
            else if (mSavedStrat.getRole() == strat.getRole()) // man is real attacker
            {
                if (owner == PuckOwner.PLAYER) // his friend own puck
                {
                    var friend = getManById(world.Puck.OwnerHockeyistId, world);
                    if (canDefenderPlayAttacker(friend, world, PuckOwner.HOCKEYIST))
                    {
                        strat.setAlternativeBehaviour(true); // lets break down enemies
                    }
                }
                else if (owner != PuckOwner.PLAYER)
                {
                    strat.setAlternativeBehaviour(false); 
                }
            }
        }

        private bool hasNoEnemyFront(Hockeyist self, World world)
        {
            foreach (var enemy in world.Hockeyists)
            {
                if (!enemy.IsTeammate && enemy.Type != HockeyistType.Goalie)
                {
                    if (strat.isOpponentSideLeft(world))
                    {
                        if (enemy.X < self.X - self.Radius)
                            return false;
                    }
                    else
                    {
                        if (enemy.X > self.X + self.Radius)
                            return false;
                    }
                }
                
            }
            return true;
        }

        private bool canDefenderPlayAttacker(Hockeyist self, World world, PuckOwner owner)
        {
            return owner == PuckOwner.HOCKEYIST
                       && (hasNoEnemyInHalfField(self, world) || hasNoEnemyFront(self, world));
        }

        private bool hasNoEnemyInHalfField(Hockeyist self, World world)
        {
            var enemy = strat.getNearestEnemy(self, world);
            return self.GetDistanceTo(enemy) > world.Width / 2;
        }

        private Hockeyist getManById(long id, World world)
        {
            foreach (var man in world.Hockeyists)
            {
                if (man.Id == id)
                    return man;
            }
            return null;
        }
    }
}
