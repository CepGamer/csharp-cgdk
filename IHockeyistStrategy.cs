using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.CodeGame.CodeHockey2014.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeHockey2014.DevKit.CSharpCgdk
{
    public abstract class IHockeyistStrategy
    {
        public enum Role
        {
            DEFENCE,
            ATTACK,
            UNIVERSAL
        } 

        protected Points StrikePlace;
        protected Point StrikeDestination;
        protected Point Direction;
        protected Constants Constant;
        protected Dictionary<Points, Point> StrikePoints;
        protected Role mRole;
        protected bool mAlternativeBehaviour;

        public Role getRole()
        {
            return mRole;
        }
        public void setAlternativeBehaviour(bool flag)
        {
            mAlternativeBehaviour = flag;
        }

        public IHockeyistStrategy(Game game, double hitPointRadius, double nearNetStrike)
        {
            Constant = new Constants(hitPointRadius, nearNetStrike);
            StrikeDestination = new Point(0, 0);
            Direction = new Point(0, 0);
            StrikePoints = new Dictionary<Points, Point>();

            double coef = hitPointRadius / 2;
            double vertK = 1.5;
            double hortK = 1.65;

            //  Ударные позиции
            Point TopLeft = new Point(game.GoalNetWidth + game.GoalNetHeight + coef * hortK, game.GoalNetTop - coef * vertK);
            Point TopRight = new Point(game.WorldWidth - game.GoalNetWidth - game.GoalNetHeight - coef * hortK, game.GoalNetTop - coef * vertK);
            Point BottomLeft = new Point(game.GoalNetWidth + game.GoalNetHeight + coef * hortK, game.GoalNetTop + game.GoalNetHeight + coef * vertK);
            Point BottomRight = new Point(game.WorldWidth - game.GoalNetWidth - game.GoalNetHeight - coef * hortK, game.GoalNetTop + game.GoalNetHeight + coef * vertK);

            StrikePoints.Add(Points.TOPLEFT, TopLeft);
            StrikePoints.Add(Points.TOPRIGHT, TopRight);
            StrikePoints.Add(Points.BOTTOMLEFT, BottomLeft);
            StrikePoints.Add(Points.BOTTOMRIGHT, BottomRight);
            mAlternativeBehaviour = false;
        }

        public abstract void execute(MyStrategy hockeyistStrat, Hockeyist self, World world, Game game, Move move);

        protected bool AbleToMove(Hockeyist check)
        {
            return !(check.State == HockeyistState.KnockedDown || check.State == HockeyistState.Resting);
        }

        protected double getBar(Side bar, Player opponent)
        {
            if (bar == Side.BOTTOM)
            {
                return opponent.NetBottom;
            }
            else
            {
                return opponent.NetTop;
            }
        }

        protected Side getOtherBar(Unit self, Player opponent)
        {
            Side toRet;
            if (Math.Abs(self.Y - opponent.NetBottom) > Math.Abs(self.Y - opponent.NetTop))
                toRet = Side.BOTTOM;
            else toRet = Side.TOP;

            return toRet;
        }

        protected bool insideArea(Unit striker, Point x, double radius)
        {
            return (striker.GetDistanceTo(x.X, x.Y) < radius);
        }

        public PuckOwner havePuck(Hockeyist self, World world)
        {
            PuckOwner havePuck = PuckOwner.ENEMY;
            var ownerId = world.Puck.OwnerHockeyistId;
            if (ownerId == -1)
                havePuck = PuckOwner.NOBODY;
            else if (ownerId != self.Id)
                foreach (var i in world.Hockeyists)
                {
                    if (i.Id == ownerId)
                    {
                        havePuck = i.IsTeammate ? PuckOwner.PLAYER : PuckOwner.ENEMY;
                        break;
                    }
                }
            else havePuck = PuckOwner.HOCKEYIST;
            return havePuck;
        }
        
        /// <summary>
        /// Convert 2 Side structures to Points structure.
        /// </summary>
        /// <param name="vertical">Vertical side: TOP or BOTTOM</param>
        /// <param name="horizontal">Horizontal side: LEFT or RIGHT</param>
        /// <returns>Points struct</returns>
        protected Points getPoints(Side vertical, Side horizontal)
        {
            return (vertical == Side.TOP) ? (horizontal == Side.LEFT ? Points.TOPLEFT : Points.TOPRIGHT) : (horizontal == Side.LEFT ? Points.BOTTOMLEFT : Points.BOTTOMRIGHT);
        }

        protected Point pointsToPoint(Points x)
        {
            return StrikePoints[x];
        }

        protected bool canGet(Hockeyist getter, Unit getting, Game game)
        {
            return (getter.GetDistanceTo(getting) <= game.StickLength && Math.Abs(getter.GetAngleTo(getting)) <= game.StickSector / 2);
        }

        protected void moveTo(Hockeyist who, Move move, Point x)
        {
            move.Action = ActionType.CancelStrike;
            move.Turn = who.GetAngleTo(x.X, x.Y);
            move.SpeedUp = 1 - Math.Abs(move.Turn / Math.PI);
        }

        protected Hockeyist findNearest(World world, Game game)
        {
            Hockeyist nearest = null;
            foreach (var item in world.Hockeyists)
            {
                if (!item.IsTeammate)
                {
                    continue;
                }
                if (nearest == null)
                {
                    nearest = item;
                }
                else
	            {
                    var nearestIs = Math.Abs(nearest.Y - world.GetOpponentPlayer().NetFront);
                    var curr = Math.Abs(item.Y - world.GetOpponentPlayer().NetFront);
                    if (nearestIs > game.GoalNetHeight && curr > game.GoalNetHeight && curr < nearestIs)
	                {
                        nearest = item;
	                }
	            }
            }
            return nearest;
        }

        public Hockeyist getNearestTeammate(Hockeyist self, World world)
        {
            double minDistance = world.Width * 5;
            Hockeyist nearest = self;
            foreach (var man in world.Hockeyists)
            {
                if (man.IsTeammate && (man.Type != HockeyistType.Goalie)
                       && self.GetDistanceTo(man) < minDistance
                       && self.Id != man.Id)
                {
                    minDistance = self.GetDistanceTo(man);
                    nearest = man;
                }
            }
            return nearest;
        }

        public Hockeyist getNearestEnemy(Hockeyist self, World world)
        {
            double minDistance = Double.MaxValue;
            Hockeyist nearest = null;
            foreach (var man in world.Hockeyists)
            {
                if (!man.IsTeammate && man.Type != HockeyistType.Goalie
                       && self.GetDistanceTo(man) < minDistance)
                {
                    minDistance = self.GetDistanceTo(man);
                    nearest = man;
                }
            }
            return nearest;
        }

        public bool isOpponentSideLeft(World world)
        {
            return world.GetOpponentPlayer().NetLeft < world.GetMyPlayer().NetLeft;
        }
    }
}
