using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.CodeGame.CodeHockey2014.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeHockey2014.DevKit.CSharpCgdk
{
    public class Defenceman : IHockeyistStrategy
    {
        protected enum DefenseState
        {
            GoBack2Net,
            KeepCalm,
            Attack,
            TakePass,
            Rest
        }
        protected DefenseState mState;
        public Defenceman(Game game, double hitPointRadius, double nearNetStrike) : base(game, hitPointRadius, nearNetStrike) 
        {
            mRole = Role.DEFENCE;
            mState = DefenseState.GoBack2Net;
        }
        public override void execute(MyStrategy strat, Hockeyist self, World world, Game game, Move move)
        {
            if (!AbleToMove(self))
            {
                move.Action = ActionType.CancelStrike;
                mState = DefenseState.Rest;
                
            }
            checkForHavingPuck(self, world);

            switch (mState)
            {
                case DefenseState.GoBack2Net:
                    stateGoBack2Net(self, world, game, move);
                    break;
                case DefenseState.KeepCalm:
                    stateKeepCalm(self, world, game, move);
                    break;
                case DefenseState.Attack:
                    stateAttack(self, world, game, move);
                    break;
                case DefenseState.TakePass:
                    stateTakePass(self, world, game, move);
                    break;

                default:
                    mState = DefenseState.GoBack2Net;
                    break;
            }

            postamble(self, world, move, game);      
        }

        private Point getDestination(Hockeyist self, World world, Game game)
        {
            Point result = new Point(0,0); 
            var myGoalkeeper = getGoalkeeper(world);
            if (myGoalkeeper == null)
            {
                return ownNetCenter(world);
            }

            var isEnemySideLeft = isOpponentSideLeft(world);
            
            var shift = myGoalkeeper.Y - world.GetMyPlayer().NetTop;
            var isTopFree = shift > (world.GetMyPlayer().NetBottom - world.GetMyPlayer().NetTop) / 2;
            var startPointY = isTopFree?  world.GetMyPlayer().NetTop + self.Radius 
                                          : world.GetMyPlayer().NetBottom - self.Radius;
            var startPointX = isEnemySideLeft? world.GetMyPlayer().NetLeft - 2 * self.Radius
                                               : world.GetMyPlayer().NetRight + 2 * self.Radius;
            var startPoint = new Point(startPointX, startPointY);

            var puckPoint = new Point(world.Puck.X, world.Puck.Y);

            var ratio = availableRadius(world, game) /  getDistanceFromTo(startPoint, puckPoint);

            var resultX = startPointX + (isEnemySideLeft ? -1 : 1) * ratio * (Math.Abs(startPointX - puckPoint.X));
            var resultY = startPointY + (puckPoint.Y > startPointY? 1 : -1) * ratio * (Math.Abs(startPointY - puckPoint.Y));

            return new Point(resultX, resultY);
        }

        private double availableRadius(World world, Game game)
        {
            return game.GoalNetHeight / 2;
        }

        private Hockeyist getGoalkeeper(World world)
        {
            foreach (var item in world.Hockeyists)
            {
                if (item.Type == HockeyistType.Goalie && item.IsTeammate)
                {
                    return item;
                }
            }
            return null; // to make compiler happy
        }

        private double getDistanceFromTo(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        private void stateGoBack2Net(Hockeyist self, World world, Game game, Move move)
        {
            var bestDefensePoint = getDestination(self, world, game);
            if (self.GetDistanceTo(bestDefensePoint.X, bestDefensePoint.Y) <= self.Radius)
            {
                move.Turn = self.GetAngleTo(world.Puck);
                move.SpeedUp = -0.5D;

                mState = DefenseState.KeepCalm;
            }
            else
            {
                var angle = self.GetAngleTo(bestDefensePoint.X, bestDefensePoint.Y);
                var targetBehind = Math.Abs(angle) >= 3 * Math.PI / 4;
                move.Turn = targetBehind? -angle : angle;
                move.SpeedUp = targetBehind ? -1D + 0.3 * Math.Abs(move.Turn / Math.PI) : 1D - Math.Abs(angle / Math.PI);
            }
            if (isPuckInDanger(self, world))
            {
                mState = DefenseState.Attack;
            }

            if (canGet(self, world.Puck, game) && havePuck(self, world) != PuckOwner.PLAYER)
                move.Action = ActionType.Strike;
            else
                move.Action = ActionType.CancelStrike;
        }

        private void stateKeepCalm(Hockeyist self, World world, Game game, Move move)
        {
            move.Turn = self.GetAngleTo(world.Puck);
            move.SpeedUp = 0.07D;
            if (canGet(self, world.Puck, game) && havePuck(self, world) != PuckOwner.PLAYER)
            {
                if (self.GetDistanceTo(getNearestEnemy(self, world)) > game.WorldWidth / 3)
                {
                    move.Action = ActionType.TakePuck;
                }
                else
                {
                    move.Action = ActionType.Strike;
                }
                mState = DefenseState.GoBack2Net;
            }
            else
            {
                var bestDefensePoint = getDestination(self, world, game);
                move.Action = ActionType.CancelStrike;
                if (self.GetDistanceTo(world.Puck) <= self.Radius * 7 
                        /*&& self.GetDistanceTo(getNearestEnemy(self, world)) <= self.Radius * 9*/)
                {
                    mState = DefenseState.Attack;
                    stateAttack(self, world, game, move);
                    
                }
                else if (self.GetDistanceTo(bestDefensePoint.X, bestDefensePoint.Y) <= self.Radius)
                {
                    mState = DefenseState.GoBack2Net;
                    stateGoBack2Net(self, world, game, move);
                }
            }
        }

        private void stateAttack(Hockeyist self, World world, Game game, Move move)
        {
            if (isPuckInDanger(self, world))
            {
                moveTo(self, move, new Point(world.Puck.X, world.Puck.Y));
                if (canGet(self, world.Puck, game))
                {
                    move.Action = ActionType.Strike;
                    mState = DefenseState.GoBack2Net;
                }
            }
            else if (havePuck(self, world) == PuckOwner.PLAYER)
            {
                mState = DefenseState.GoBack2Net;
                stateGoBack2Net(self, world, game, move);
            }
            else if (canGet(self, getNearestEnemy(self, world), game))
            {
                move.Action = ActionType.Strike;
                mState = DefenseState.GoBack2Net;
            }
            else
            {
                mState = DefenseState.GoBack2Net;
                stateGoBack2Net(self, world, game, move);
            }
        }

        private void stateTakePass(Hockeyist self, World world, Game game, Move move)
        {
            var friend = getNearestTeammate(self, world);
            var angle = 0D;
            Point upCenter = new Point(0, 0);
            Point downCenter = new Point(0, world.Height);
            double lX = (2 * (world.Width / 2) + self.X) / 3;

            if (self.GetDistanceTo(friend) < self.GetDistanceTo(getNearestEnemy(self, world)))
            {
                self.GetAngleTo(friend);
            }
            else
            {
                // you can walk throw her
                angle = self.GetAngleTo(lX, (self.Y >= world.Height / 2)? upCenter.Y : downCenter.Y);
            }
                        
            move.Turn = angle;
            if (Math.Abs(angle) < game.PassSector / 3)
            {
                move.SpeedUp = 0.1D;
                move.Action = ActionType.Pass;
                mState = DefenseState.GoBack2Net;
            }
            else
            {
                move.SpeedUp = 0.8D;
                move.Action = ActionType.None;
            }
            
        }

        private void checkForGoal(World world)
        {
            if (world.GetMyPlayer().IsJustMissedGoal || world.GetMyPlayer().IsJustScoredGoal)
            {
                mState = DefenseState.GoBack2Net;
            }
        }

        private void checkForHavingPuck(Hockeyist self, World world)
        {
            if (havePuck(self, world) == PuckOwner.HOCKEYIST)
            {
                mState = DefenseState.TakePass;
            }
        }

        private bool isPuckInDanger(Hockeyist self, World world)
        {
            return (self.GetDistanceTo(world.Puck) <= self.Radius * 10 && havePuck(self, world) != PuckOwner.PLAYER);
        }

        private bool isStaringInOwnNet(Hockeyist self, World world)
        {
            Point netCenter = ownNetCenter(world);
            return Math.Abs(self.GetAngleTo(netCenter.X, netCenter.Y)) < Math.PI / 2;
        }

        private void postamble(Hockeyist self, World world, Move move, Game game)
        {
            var canGetPuck = canGet(self, world.Puck, game);
            if (canGet(self, getNearestEnemy(self, world), game) && !canGetPuck)
            {
                move.Action = ActionType.Strike;
            }
            if (isStaringInOwnNet(self, world) && canGetPuck && move.Action == ActionType.Strike)
            {
                move.Action = ActionType.TakePuck;
            }
        }

        private Point ownNetCenter(World world)
        {
            return new Point(world.GetMyPlayer().NetLeft, (world.GetMyPlayer().NetTop + world.GetMyPlayer().NetBottom) / 2);
        }


   
    }
    
} //