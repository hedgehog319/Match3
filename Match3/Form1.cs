using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Match3.Properties;

namespace Match3
{
    public partial class Form1 : Form
    {
        private const int MaxGameTime = 60;

        private const int BoardSize = 8;
        private static readonly Random Random = new Random();

        private readonly Bitmap[] _gemTypes =
        {
            new Bitmap(Resources.blue_gem),
            new Bitmap(Resources.red_gem),
            new Bitmap(Resources.green_gem),
            new Bitmap(Resources.purple_gem),
            new Bitmap(Resources.yellow_gem)
        };

        private readonly Gem[,] _map = new Gem[BoardSize, BoardSize];
        private int _gameTime;
        private bool _isAnimated;
        private bool _reverseSwap;
        private int _score;
        private Gem _selectedGem;

        private readonly Timer _timer = new Timer
        {
            Interval = 1000
        };


        public Form1()
        {
            InitializeComponent();


            GameTime = MaxGameTime;
            _timer.Tick += (s, e) =>
            {
                if (GameTime > 0)
                    GameTime--;
                else
                    EndGame();
            };

            GameBoard.Paint += GameBoardOnPaint;
        }

        private int Score
        {
            get => _score;
            set
            {
                if (_score == value) return;

                _score = value;
                LabelScore.Text = $@"Score: {_score}";
            }
        }

        private int GameTime
        {
            get => _gameTime;
            set
            {
                if (_gameTime == value) return;

                _gameTime = value;
                LabelTime.Text = $@"Time: {_gameTime / 60:00}:{_gameTime % 60:00}";
            }
        }

        private void GameBoardOnPaint(object sender, PaintEventArgs e)
        {
            foreach (var gem in _map)
            {
                if (gem.IsActive)
                    e.Graphics.FillRectangle(
                        new SolidBrush(Color.Chartreuse),
                        gem.Box
                    );

                if (gem.Image != null)
                    e.Graphics.DrawImage(gem.Image, gem.Box);
            }
        }

        private Gem GetRandomGem(int x, int y)
        {
            var bitMap = _gemTypes[Random.Next(_gemTypes.Length)];
            return new Gem(bitMap, x, y);
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            PlayButton.Visible = false;
            LabelScore.Visible = true;
            LabelTime.Visible = true;
            GameBoard.Visible = true;

            for (var y = 0; y < BoardSize; y++)
            for (var x = 0; x < BoardSize; x++)
                _map[y, x] = GetRandomGem(x * Gem.Size, y * Gem.Size);

            while (CutLines()) FillBoard();

            GameTime = MaxGameTime;
            Score = 0;
            _timer.Start();
        }

        private void EndGame()
        {
            _timer.Stop();

            MessageBox.Show($@"Your score: {Score}", @"Game over");

            PlayButton.Visible = true;
            LabelScore.Visible = false;
            LabelTime.Visible = false;
            GameBoard.Visible = false;
        }

        private void GameBoard_MouseClick(object sender, MouseEventArgs e)
        {
            if (_isAnimated) return;

            if (_selectedGem == null)
            {
                foreach (var gem in _map)
                {
                    if (!gem.InBox(e.Location)) continue;

                    gem.IsActive = true;
                    _selectedGem = gem;
                    break;
                }
            }
            else
            {
                var i = _selectedGem.MapY;
                var j = _selectedGem.MapX;
                _selectedGem.IsActive = false;
                Gem nearGem = null;

                if (i - 1 >= 0 && _map[i - 1, j].InBox(e.Location))
                    nearGem = _map[i - 1, j];
                else if (i + 1 < BoardSize && _map[i + 1, j].InBox(e.Location))
                    nearGem = _map[i + 1, j];
                else if (j - 1 >= 0 && _map[i, j - 1].InBox(e.Location))
                    nearGem = _map[i, j - 1];
                else if (j + 1 < BoardSize && _map[i, j + 1].InBox(e.Location)) nearGem = _map[i, j + 1];

                if (nearGem != null)
                {
                    var x1 = _selectedGem.MapX;
                    var y1 = _selectedGem.MapY;

                    var x2 = nearGem.MapX;
                    var y2 = nearGem.MapY;

                    (_map[y1, x1], _map[y2, x2]) = (_map[y2, x2], _map[y1, x1]);

                    SwapAnimation(_selectedGem, nearGem);

                    _selectedGem = null;
                }
                else
                {
                    _selectedGem = null;
                }
            }

            GameBoard.Refresh();
        }

        private void SwapAnimation(Gem g1, Gem g2)
        {
            _isAnimated = true;

            const int steps = 10;
            var delta = new Point(
                (g1.X - g2.X) / steps,
                (g1.Y - g2.Y) / steps
            );

            var i = 0;
            var timer = new Timer
            {
                Interval = 50
            };
            timer.Tick += (s, e) =>
            {
                if (i >= steps)
                {
                    timer.Dispose();
                    return;
                }

                i++;

                g1.X -= delta.X;
                g1.Y -= delta.Y;

                g2.X += delta.X;
                g2.Y += delta.Y;


                GameBoard.Refresh();
            };

            timer.Disposed += (s, e) =>
            {
                _isAnimated = false;
                AfterSwap(g1, g2);
            };
            timer.Start();
        }

        private void AfterSwap(Gem g1, Gem g2)
        {
            if (_reverseSwap)
            {
                _reverseSwap = false;
                return;
            }

            if (CutLines())
            {
                GameBoard.Refresh();
                FallAnimation();
            }
            else
            {
                _reverseSwap = true;
                SwapAnimation(g1, g2);

                var x1 = g1.MapX;
                var y1 = g1.MapY;

                var x2 = g2.MapX;
                var y2 = g2.MapY;

                (_map[y1, x1], _map[y2, x2]) = (_map[y2, x2], _map[y1, x1]);
            }
        }

        private bool CutLines()
        {
            var removeMap = new bool[BoardSize, BoardSize];
            var count = 0;

            for (var y = 0; y < BoardSize; y++)
            for (var x = 0; x < BoardSize - 2; x++)
            {
                count = 0;
                for (var i = x + 1; i < BoardSize; i++)
                    if (_map[y, x].IsSame(_map[y, i])) count++;
                    else break;

                if (count >= 2)
                    for (var i = x; i <= x + count; i++)
                        removeMap[y, i] = true;
                x += count;
            }

            for (var x = 0; x < BoardSize; x++)
            for (var y = 0; y < BoardSize - 2; y++)
            {
                count = 0;
                for (var i = y + 1; i < BoardSize; i++)
                    if (_map[i, x].IsSame(_map[y, x])) count++;
                    else break;

                if (count >= 2)
                    for (var i = y; i <= y + count; i++)
                        removeMap[i, x] = true;
                else y += count;
            }

            count = 0;
            for (var y = 0; y < BoardSize; y++)
            for (var x = 0; x < BoardSize; x++)
                if (removeMap[y, x])
                {
                    _map[y, x].Image = null;
                    count++;
                }

            Score += count * 10;
            return count > 0;
        }

        private void FillBoard()
        {
            for (var x = 0; x < BoardSize; x++)
            for (var y = 0; y < BoardSize; y++)
                if (_map[y, x].IsEmpty)
                {
                    var X = _map[y, x].X;
                    var Y = _map[y, x].Y;
                    _map[y, x] = GetRandomGem(X, Y);
                }
        }

        private void FallAnimation()
        {
            _isAnimated = true;

            const int steps = 10;

            var i = 0;
            var timer = new Timer
            {
                Interval = 50
            };

            var rowCounts = new int[BoardSize];
            var last = new int[BoardSize];

            for (var x = 0; x < BoardSize; x++)
            for (var y = 0; y < BoardSize; y++)
                if (_map[y, x].IsEmpty)
                {
                    rowCounts[x]++;
                    last[x] = y;
                    _map[y, x].Y = -Gem.Size * (rowCounts[x]);

                    for (var j = y; j > 0; j--)
                        (_map[j, x], _map[j - 1, x]) = (_map[j - 1, x], _map[j, x]);
                }

            FillBoard();

            var row = 0;
            timer.Tick += (s, e) =>
            {
                for (var x = 0; x < BoardSize; x++)
                {
                    if (rowCounts[x] == 0 || row >= rowCounts[x]) continue;

                    for (var j = 0; j <= last[x]; j++) _map[j, x].Y += 5;
                }

                GameBoard.Refresh();
            };

            var latest = rowCounts.Max();
            timer.Tick += (s, e) =>
            {
                i++;
                if (i % steps == 0) row++;

                if (row >= latest) timer.Dispose();
            };

            timer.Disposed += (s, e) =>
            {
                _isAnimated = false;
                if (!CutLines()) return;

                GameBoard.Refresh();
                FallAnimation();
            };
            timer.Start();
        }
    }

    public class Gem
    {
        public const int Size = 50;
        private Rectangle _box;

        private int _x, _y;

        public Gem(Bitmap img, int x = 0, int y = 0)
        {
            X = x;
            Y = y;

            Box = new Rectangle(x, y, Size, Size);
            Image = img;
        }

        public int X
        {
            get => _x;
            set
            {
                if (_x == value) return;

                _x = value;
                _box.X = value;
            }
        }

        public int MapX => X / Size;

        public int Y
        {
            get => _y;
            set
            {
                if (_y == value) return;

                _y = value;
                _box.Y = value;
            }
        }

        public int MapY => Y / Size;

        public bool IsActive { get; set; }
        public Image Image { get; set; }

        public bool IsEmpty => Image == null;

        public Rectangle Box
        {
            get => _box;
            private set => _box = value;
        }

        public void Swap(Gem gem)
        {
            (Image, gem.Image) = (gem.Image, Image);
        }

        public bool IsSame(Gem gem)
        {
            return Image != null && Image == gem.Image;
        }

        public bool InBox(Point point)
        {
            return point.X >= X && point.X <= X + Size &&
                   point.Y >= Y && point.Y <= Y + Size;
        }
    }
}