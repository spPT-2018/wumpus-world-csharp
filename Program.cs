﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WumpusWorld
{
    // https://cis.temple.edu/~giorgio/cis587/readings/wumpus.shtml#1
    class Program
    {
        static async Task Main(string[] args)
        {

            var world = new CaveWorld(new List<Position>
            {
                new Position(0, 3)
            }, 
            new List<Position>
            {
                new Position(3, 0),
                new Position(3, 2),
                new Position(3, 3)
            }, 
            new Position(2, 2));


            await Simulate(world, TimeSpan.FromMilliseconds(250));
//            await Simulate(world, TimeSpan.FromSeconds(1));
        }

        static async Task Simulate(CaveWorld world, TimeSpan delay)
        {
            PrintMap(world);
            await Task.Delay(delay);
            
            var startPos = new Position(0, 0);
            while (true)
            {
                world.Iterate();
                PrintMap(world);
                
                if (world.MrCat.FoundGold && world.MrCat.CurrentPosition.Equals(startPos))
                    break;

                await Task.Delay(delay);
            }

            Console.WriteLine("Level completed - Mr. Cat is home with the gold");
        }
       
        static void PrintMap(CaveWorld world)
        {
            Console.Clear();
            Console.CursorTop = 0;
            Console.WriteLine("╔════╗");
            for (int y = world.WorldHeight - 1; y >= 0; y--)
            {
                Console.Write('║');
                for (int x = 0; x < world.WorldWidth; x++)
                {
                    var pos = new Position(x, y);
                    if (world.MrCat.CurrentPosition.Equals(pos))
                    {
                        Console.Write('A');
                    }
                    else if (world.WumpusAt(pos))
                    {
                        Console.Write('W');
                    }
                    else if (world.PitAt(pos))
                    {
                        Console.Write('O');
                    }
                    else if (world.GoldAt(pos) && !world.MrCat.FoundGold)
                    {
                        Console.Write('G');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('║');
                Console.WriteLine();
            }
            Console.WriteLine("╚════╝");
        }
    }
    
    
    struct Position : IEquatable<Position>
    {
        public int X;
        public int Y;

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Position other && Equals(other);
        }

        public bool Equals(Position other)
        {
            return X == other.X && Y == other.Y;
        }
    }

    struct Percepts
    {
        public bool Stench;
        public bool Breeze;
        public bool Glitter;
    }

    class Knowledge
    {
        public bool MightHaveWumpus;
        public bool MightHavePit;
    }

    class CaveWorld
    {
        
        public int WorldHeight = 4;
        public int WorldWidth = 4;
        

        private List<Position> Wumpi;
        private List<Position> Pits;
        public Position Gold;
        
        public AgentCat MrCat = new AgentCat();

        public CaveWorld(List<Position> wumpi, List<Position> pits, Position gold)
        {
            Wumpi = wumpi;
            Pits = pits;
            Gold = gold;
            
            MrCat.TellMeAboutTheWorld(WorldWidth, WorldHeight);
        }

        public void Iterate()
        {
            var percepts = GeneratePercepts();
            MrCat.PerceiveCurrentPosition(percepts);

            var agentMove = MrCat.WhereIWannaGo();
            MrCat.CurrentPosition = agentMove;
            
        }


        Percepts GeneratePercepts()
        {
            var neighbours = GetNeighbours();
            
            return new Percepts
            {
                Breeze = neighbours.Any(PitAt),
                Stench = neighbours.Any(WumpusAt),
                Glitter = MrCat.CurrentPosition.Equals(Gold)
            };
        }
        
        List<Position> GetNeighbours()
        {
            var possiblePositions = new List<Position>();

            if (MrCat.CurrentPosition.X > 0) // we can go west
            {
                possiblePositions.Add(new Position(MrCat.CurrentPosition.X - 1, MrCat.CurrentPosition.Y));
            }

            if (MrCat.CurrentPosition.X < WorldWidth - 1) // we can go east
            {
                possiblePositions.Add(new Position(MrCat.CurrentPosition.X + 1, MrCat.CurrentPosition.Y));
            }

            if (MrCat.CurrentPosition.Y > 0) // we can go south
            {
                possiblePositions.Add(new Position(MrCat.CurrentPosition.X, MrCat.CurrentPosition.Y - 1));
            }

            if (MrCat.CurrentPosition.Y < WorldHeight - 1) // we can go north
            {
                possiblePositions.Add(new Position(MrCat.CurrentPosition.X, MrCat.CurrentPosition.Y + 1));
            }

            return possiblePositions;
        }

        // Toby funcs
        public bool PitAt(Position position) => Pits.Any(pit => pit.Equals(position));
        public bool WumpusAt(Position position) => Wumpi.Any(wumpus => wumpus.Equals(position));
        public bool GoldAt(Position position) => Gold.Equals(position);
    }
    
    class AgentCat
    {
        private int WorldHeight;
        private int WorldWidth;
        
        public void TellMeAboutTheWorld(int width, int height)
        {
            WorldWidth = width;
            WorldHeight = height;
        }


        private Dictionary<Position, Percepts> PerceptedPlaces = new Dictionary<Position, Percepts>();
        private Dictionary<Position, Knowledge> KnowledgeOfPlaces = new Dictionary<Position, Knowledge>();

        public bool FoundGold = false;
        
        public Position CurrentPosition = new Position(0, 0);
        private Stack<Position> Trace = new Stack<Position>();

        public void PerceiveCurrentPosition(Percepts percepts)
        {
            PerceptedPlaces[CurrentPosition] = percepts;
            KnowledgeOfPlaces[CurrentPosition] = new Knowledge();

            if (percepts.Glitter)
            {
                FoundGold = true;
            }

            var newPlacesToGo = PossibleMoves().Where(pos => !PerceptedPlaces.ContainsKey(pos));

            foreach (var position in newPlacesToGo)
            {
                var hasKnowledge = KnowledgeOfPlaces.ContainsKey(position);

                if (hasKnowledge)
                {
                    var knowledge = KnowledgeOfPlaces[position];
                    if (!percepts.Stench && knowledge.MightHaveWumpus)
                    {
                        knowledge.MightHaveWumpus = false;
                    }
                    
                    if (!percepts.Breeze && knowledge.MightHavePit)
                    {
                        knowledge.MightHavePit = false;
                    }
                }
                else
                {
                    KnowledgeOfPlaces[position] = new Knowledge
                    {
                        MightHaveWumpus = percepts.Stench,
                        MightHavePit = percepts.Breeze
                    };
                }
            }
        }
        
        public Position WhereIWannaGo()
        {
            if (FoundGold)
            {
                if (Trace.Count == 0)
                {
                    return new Position(0, 0);
                }
                return Trace.Pop();
            }
            else
            {
                // Find gold 'n kill wumpi
                
                var placesToGo = PossibleMoves();
                
                var placesIveBeen = placesToGo.Where(pos => PerceptedPlaces.ContainsKey(pos));
                var newPlacesToGo = placesToGo.Where(pos => !PerceptedPlaces.ContainsKey(pos));
                
                var safeNewPlacesToGo = newPlacesToGo.Where(IKnowItIsSafe);

                if (safeNewPlacesToGo.Any())
                {
                    var move = safeNewPlacesToGo.First();
                    Trace.Push(move);
                    return move;
                }


                var safePlacesToGo = placesToGo.Where(IKnowItIsSafe);

                if (safePlacesToGo.Any())
                {
                    var move = safePlacesToGo.First();
                    Trace.Push(move);
                    return move;
                }

                Console.WriteLine("I might die now :'(");
                var dangerousMove = newPlacesToGo.Any() ? newPlacesToGo.First() : placesToGo.First();
                Trace.Push(dangerousMove);
                return dangerousMove;
            }
        }

        private bool IKnowItIsSafe(Position position)
        {
            return KnowledgeOfPlaces.ContainsKey(position) &&
                   !KnowledgeOfPlaces[position].MightHaveWumpus &&
                   !KnowledgeOfPlaces[position].MightHavePit;
        }
        

        private List<Position> PossibleMoves()
        {
            var possiblePositions = new List<Position>();

            if (CurrentPosition.X > 0) // we can go west
            {
                possiblePositions.Add(new Position(CurrentPosition.X - 1, CurrentPosition.Y));
            }

            if (CurrentPosition.X < WorldWidth - 1) // we can go east
            {
                possiblePositions.Add(new Position(CurrentPosition.X + 1, CurrentPosition.Y));
            }

            if (CurrentPosition.Y > 0) // we can go south
            {
                possiblePositions.Add(new Position(CurrentPosition.X, CurrentPosition.Y - 1));
            }

            if (CurrentPosition.Y < WorldHeight - 1) // we can go north
            {
                possiblePositions.Add(new Position(CurrentPosition.X, CurrentPosition.Y + 1));
            }

            return possiblePositions;
        }
        
    }
}