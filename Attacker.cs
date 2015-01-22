using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.CodeGame.CodeHockey2014.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeHockey2014.DevKit.CSharpCgdk
{
    public class Attacker : IHockeyistStrategy
    {
        public Attacker(Game game, double hitPointRadius, double nearNetStrike) : base(game, hitPointRadius, nearNetStrike) 
        {
            mRole = Role.ATTACK;
        }

        public override void execute(MyStrategy strat, Hockeyist self, World world, Game game, Move move)
        {
            double k;
            var opponent = world.GetOpponentPlayer();
            if (!AbleToMove(self))
            {
                move.Action = ActionType.CancelStrike;
                return;
            }

            //  Определили, у нас ли шайба
            switch (havePuck(self, world))
	        {
                case PuckOwner.PLAYER:
                    if (!mAlternativeBehaviour)
                    {
                        // набигаем на пас
                        moveToEnemyNet(self, game, move, opponent);
                    }
                    else
                    {
                        // защитник стал временным нападающим, мы мешаем врагам!
                        double distance = 2000D;
                        foreach (var item in world.Hockeyists)
                        {
                            if (item.IsTeammate || item.Type == HockeyistType.Goalie)
                            {
                                continue;
                            }
                            if (distance > self.GetDistanceTo(item))
                            {
                                if (canGet(self, item, game))
                                {
                                    if (!canGet(self, world.Puck, game))
                                        move.Action = ActionType.Strike;
                                    else
                                        continue;
                                }
                                else
                                {
                                    move.Turn = self.GetAngleTo(item);
                                    move.SpeedUp = 1 - Math.Abs(move.Turn / 3);
                                }
                                distance = self.GetDistanceTo(item);
                            }
                        }
                    }
                    
                    break;

                case PuckOwner.HOCKEYIST:
                    var nearest = findNearest(world, game);
                    var corr = self.GetAngleTo(nearest);
                    if (nearest != null && nearest.Id != self.Id && corr < 1)
                    {
                        move.Turn = corr;
                        move.PassAngle = corr;
                        move.PassPower = self.GetDistanceTo(nearest) / game.WorldWidth;
                        move.Action = ActionType.Pass;
                    }
                    //  Определяем направление удара
                    if (insideArea(self, pointsToPoint(StrikePlace), Constant.HitPointRadius) || 
                        (world.TickCount > 6000 && world.GetOpponentPlayer().GoalCount == 0))
                    {
                        double correction;

                        correction = self.GetAngleTo(StrikeDestination.X, StrikeDestination.Y);

                        if (Math.Abs(correction) < 0.05 || self.State == HockeyistState.Swinging)
                        {
                            var distToStrikeDest = self.GetDistanceTo(StrikeDestination.X, StrikeDestination.Y);
                            var swingCoef = distToStrikeDest < Constant.NearNetStrike ? 0 :
                                distToStrikeDest < Constant.NearNetStrike * 2 ? 0.5 : 0.75;

                            if (self.SwingTicks >= game.MaxEffectiveSwingTicks * swingCoef)
                            {
                                move.Action = ActionType.Strike;
                            }
                            else
                            {
                                move.Action = ActionType.Swing;
                            }
                        }
                        else
                        {
                            move.Turn = correction;
                            move.SpeedUp = -0.5;
                            move.Action = ActionType.CancelStrike;
                        }
                    }
                    //  Направление подхода к зоне оппонента
                    else
                    {
                        moveToEnemyNet(self, game, move, opponent);
                    }
                    break;
                case PuckOwner.NOBODY:
                case PuckOwner.ENEMY:
                default:
                    k = 10 * self.GetDistanceTo(world.Puck) / world.Width;
                    move.Turn = self.GetAngleTo(world.Puck.X + world.Puck.SpeedX * k, world.Puck.Y + world.Puck.SpeedY * k);
                    move.SpeedUp = Math.Max(0, 1 - Math.Abs(move.Turn / 3) - Math.Min(1D, 5D / self.GetDistanceTo(world.Puck)));
                    if (canGet(self, world.Puck, game))
                        move.Action = (havePuck(self, world) == PuckOwner.NOBODY) ? ActionType.TakePuck : ActionType.Strike;
                    else
                        move.Action = ActionType.CancelStrike;
                    break;
            }
        }

        private void moveToEnemyNet(Hockeyist self, Game game, Move move, Player opponent)
        {
            var k = 0.75;
            Side vertical, horizontal;

            horizontal = opponent.NetBack < game.WorldWidth / 2 ? Side.LEFT : Side.RIGHT;
            vertical = (self.Y > game.WorldHeight / 2) ? Side.BOTTOM : Side.TOP;

            StrikePlace = getPoints(vertical, horizontal);
            Direction = pointsToPoint(StrikePlace);
            StrikeDestination.Y = (vertical == Side.TOP) ? opponent.NetBottom : opponent.NetTop;

            StrikeDestination.X = opponent.NetBack * (1 - k) + opponent.NetFront * k;

            move.Turn = self.GetAngleTo(Direction.X, Direction.Y);
            move.SpeedUp = 1 - Math.Abs(move.Turn / 3);
            move.Action = ActionType.CancelStrike;
        }
    }
}
