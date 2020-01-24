using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private PixelModel[,] _matrix;
        private IList<GrainModel> _inclusionsPixels;
        private IList<GrainModel> _seedPixels;
        private IList<GrainModel> _borderPixels;
        private IList<GrainModel> _grainBorderPixels;
        private Graphics _graphic;
        private bool _modelCalculated;

        public int xDimension;
        public int yDimension;
        public Random rand;

        public Form1()
        {
            InitializeComponent();
            rand = new Random();

            _seedPixels = new List<GrainModel>();
            _inclusionsPixels = new List<GrainModel>();
            _borderPixels = new List<GrainModel>();
            _grainBorderPixels = new List<GrainModel>();
            _graphic = panel1.CreateGraphics();

            comboBox1.DataSource = Enumerable.Range(0, 8).ToList();
            comboBox2.DataSource = new List<string>()
            {
                "",
                Metadata.InclusionType.Circular.ToString(),
                Metadata.InclusionType.Square.ToString(),
            };
            comboBox3.DataSource = new List<string>()
            {
                "",
                Metadata.StructureType.Substructure.ToString(),
                Metadata.StructureType.DualPhase.ToString(),
            };
            comboBox4.DataSource = Enumerable.Range(0, 4).ToList();
        }

        private void MatrixGenerate_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox1.Text, out xDimension))
            {
                MessageBox.Show("Wrong Input", "Dimension x", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(textBox2.Text, out yDimension))
            {
                MessageBox.Show("Wrong Input", "Dimension y", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_matrix == null)
            {
                InitializeMatrix(xDimension, yDimension);
            }
            else
            {
                ResetMatrix(true);
            }

            RedrawGraphic();
            _seedPixels.Clear();
            _inclusionsPixels.Clear();
            _borderPixels.Clear();
            _grainBorderPixels.Clear();
            listBox1.DataSource = null;
            listBox2.DataSource = null;
            textBox3.Text = string.Empty;
        }

        private void GrainsGenerate_Click(object sender, EventArgs e)
        {
            if (_seedPixels.Any())
            {
                return;
            }

            if (!int.TryParse(comboBox1.Text, out var seedsCount))
            {
                MessageBox.Show("Wrong Input", "Number of grains", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            while (_seedPixels.Count < seedsCount)
            {
                var x = rand.Next(xDimension - 1);
                var y = rand.Next(yDimension - 1);

                var pixelModel = GetPixel(x, y);
                if (pixelModel == null || pixelModel.State != Metadata.PixelState.Default)
                {
                    continue;
                }
                _seedPixels.Add(new GrainModel()
                {
                    X = x,
                    Y = y,
                    State = (Metadata.PixelState)_seedPixels.Count + 1,
                });
            }

            ApplyPixelsStatesChange(_seedPixels);

            if (listBox1.Items.Count == 0)
            {
                listBox1.DataSource = _seedPixels.Select(x => x.State).ToList();
            }

            if (listBox2.Items.Count == 0)
            {
                listBox2.DataSource = _seedPixels.Select(x => x.State).ToList();
            }
        }

        private void InclusionsGenerate_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox5.Text, out var numberOfInclusions))
            {
                MessageBox.Show("Wrong Input", "Number Of Inclusions", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Enum.TryParse(comboBox2.Text, out Metadata.InclusionType typeOfInclusion))
            {
                MessageBox.Show("Wrong Input", "Type Of Inclusion", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(textBox6.Text, out var sizeOfInclusions))
            {
                MessageBox.Show("Wrong Input", "Size Of Inclusions", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var newInclusions = new List<GrainModel>();

            if (_modelCalculated)
            {
                CalculateBoarderPixels();

                while (newInclusions.Count < numberOfInclusions)
                {
                    var number = rand.Next(_borderPixels.Count() - 1);
                    var pixel = _borderPixels[number];
                    if (_inclusionsPixels.Any(x => x.X == pixel.X && x.Y == pixel.Y && x.State == pixel.State))
                    {
                        continue;
                    }

                    pixel.State = Metadata.PixelState.Inclusion;
                    pixel.Block = true;
                    newInclusions.Add(pixel);
                    _borderPixels.Remove(pixel);
                }
            }
            else
            {
                while (newInclusions.Count < numberOfInclusions)
                {
                    var x = rand.Next(xDimension - 1);
                    var y = rand.Next(yDimension - 1);

                    var pixelModel = GetPixel(x, y);
                    if (pixelModel == null || pixelModel.State != Metadata.PixelState.Default || pixelModel.Block)
                    {
                        continue;
                    }

                    newInclusions.Add(new GrainModel()
                    {
                        State = Metadata.PixelState.Inclusion,
                        X = x,
                        Y = y,
                        Block = true
                    });
                }
            }

            foreach (var inclusionPixel in newInclusions)
            {
                for (var x = inclusionPixel.X - sizeOfInclusions; x <= inclusionPixel.X + sizeOfInclusions; x++)
                {
                    for (var y = inclusionPixel.Y - sizeOfInclusions; y <= inclusionPixel.Y + sizeOfInclusions; y++)
                    {
                        if (!CheckIfCoordinatesCorrect(x, y))
                        {
                            continue;
                        }

                        if (typeOfInclusion == Metadata.InclusionType.Circular)
                        {
                            var distance = Math.Sqrt(Math.Pow(inclusionPixel.X - x, 2) + Math.Pow(inclusionPixel.Y - y, 2));
                            if (Math.Floor(distance) > sizeOfInclusions)
                            {
                                continue;
                            }
                        }

                        ApplyPixelStateChange(x, y, inclusionPixel.State, inclusionPixel.Block);
                    }
                }
            }

            newInclusions.ForEach(x => _inclusionsPixels.Add(x));
        }

        private void GrowthGenerate_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                BoundaryShapeControl();
            }
            else
            {
                SimpleGrainGrowth();
            }
        }

        private void GrainsSelectionGenerate_Click(object sender, EventArgs e)
        {
            if (!Enum.TryParse(comboBox3.Text, out Metadata.StructureType structureType))
            {
                MessageBox.Show("Wrong Input", "Structure Type", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selectedStates = listBox1.SelectedItems.Cast<Metadata.PixelState>().ToList();

            for (var x = 0; x < xDimension; x++)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    var pixelModel = GetPixel(x, y);

                    if (structureType == Metadata.StructureType.Substructure && selectedStates.Any(z => pixelModel.State == z))
                    {
                        ApplyPixelStateChange(x, y, true);
                        continue;
                    }

                    if (structureType == Metadata.StructureType.DualPhase && selectedStates.Any(z => pixelModel.State == z))
                    {
                        ApplyPixelStateChange(x, y, Metadata.PixelState.DualPhase, true);
                        continue;
                    }


                }
            }

            ResetMatrix();
            RedrawGraphic();
            _seedPixels.Clear();
            _modelCalculated = false;
        }

        private void GrainsBoundariesGenerate_Click(object sender, EventArgs e)
        {
            if (_grainBorderPixels.Any())
            {
                return;
            }

            var selectedStates = listBox2.SelectedItems.Cast<Metadata.PixelState>().ToList();

            if (!int.TryParse(comboBox4.Text, out var size))
            {
                MessageBox.Show("Wrong Input", "Size", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CalculateBoarderPixels(true);
            foreach (var borderPixel in _borderPixels)
            {
                for (int xN = -1 * size; xN <= 1 * size; xN++)
                {
                    for (int yN = -1 * size; yN <= 1 * size; yN++)
                    {
                        if (xN == 0 && yN == 0)
                        {
                            continue;
                        }

                        var pixel = GetPixel(borderPixel.X + xN, borderPixel.Y + yN);
                        if (pixel == null || pixel.State == Metadata.PixelState.Inclusion) // if you want to include inclusion inside border if (pixel == null)
                        {
                            continue;
                        }

                        if (selectedStates.Any(z => pixel.State == z))
                        {
                            _grainBorderPixels.Add(new GrainModel()
                            {
                                X = borderPixel.X + xN,
                                Y = borderPixel.Y + yN,
                                State = borderPixel.State,
                                Block = true
                            });
                        }
                    }
                }
            }

            ResetMatrix(true);
            ApplyPixelsStatesChange(_grainBorderPixels);
            RedrawGraphic();
        }

        private void CalculatePercentage_Click(object sender, EventArgs e)
        {
            var all = 0;
            var gb = 0;
            for (var x = 0; x < xDimension; x++)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    var pixel = GetPixel(x, y);
                    if (pixel.State == Metadata.PixelState.Border)
                    {
                        gb++;
                    }

                    all++;
                }
            }

            var asd = (decimal)gb / (decimal)all;
            textBox3.Text = asd.ToString("0.00%");
        }

        private void txtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {

            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            {
                if (openFileDialog1.FileName != "")
                {
                    switch (openFileDialog1.FilterIndex)
                    {
                        case 1:
                            OpenTxt(openFileDialog1.FileName);
                            break;

                        case 2:
                            OpenBmp(openFileDialog1.FileName);
                            break;
                    }
                }
            }
        }

        private void bmpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {

            }
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            if (saveFileDialog1.FileName != "")
            {
                switch (saveFileDialog1.FilterIndex)
                {
                    case 1:
                        SaveToTxt(saveFileDialog1.FileName);
                        break;

                    case 2:
                        SaveToBmp(saveFileDialog1.FileName);
                        break;
                }
            }
        }

        private void OpenTxt(string fileName)
        {
            using (var file = new System.IO.StreamReader(fileName, false))
            {
                var line = file.ReadLine();
                var elements = line.Split('\t');
                xDimension = int.Parse(elements[0]);
                yDimension = int.Parse(elements[1]);
                InitializeMatrix(xDimension, yDimension);
                while (file.Peek() > 0)
                {
                    line = file.ReadLine();
                    elements = line.Split('\t');

                    var x = int.Parse(elements[0]);
                    var y = int.Parse(elements[1]);
                    var pixelState = (Metadata.PixelState)int.Parse(elements[3]);

                    ApplyPixelStateChange(x, y, pixelState);
                }
            }
        }

        private void OpenBmp(string fileName)
        {
            var bitmap = new Bitmap(fileName);
            for (var x = 0; x < xDimension; x++)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    var color = new SolidBrush(pixel);
                    _graphic.FillRectangle(color, x, y, 1, 1);
                }
            }
        }

        private void SaveToTxt(string fileName)
        {
            using (var file = new System.IO.StreamWriter(fileName, false))
            {
                file.WriteLine($"{xDimension}\t{yDimension}");
                for (var x = 0; x < xDimension; x++)
                {
                    for (var y = 0; y < yDimension; y++)
                    {
                        file.WriteLine($"{x}\t{y}\t{0}\t{(int)GetPixel(x, y).State}");
                    }
                }
            }
        }

        private void SaveToBmp(string fileName)
        {
            var bitmap = new Bitmap(xDimension, yDimension);
            for (var x = 0; x < xDimension; x++)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    var pixel = GetPixel(x, y);
                    var brushColor = Utils.GetColorBaseOnPixelState(pixel.State);
                    var pen = new Pen(brushColor);
                    bitmap.SetPixel(x, y, pen.Color);
                }
            }

            bitmap.Save(fileName, ImageFormat.Bmp);
        }









        //methods for matrix and graphic
        private void InitializeMatrix(int xDim, int yDim)
        {
            _modelCalculated = false;

            _matrix = new PixelModel[xDimension, yDimension];
            for (var x = 0; x < xDim; x++)
            {
                for (var y = 0; y < yDim; y++)
                {
                    _matrix[x, y] = new PixelModel();
                }
            }
        }
        private void ResetMatrix(bool includeBlocked = false)
        {
            _modelCalculated = false;

            for (var x = 0; x < xDimension; x++)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    var pixel = GetPixel(x, y);
                    if (pixel == null)
                    {
                        _matrix[x, y] = new PixelModel();
                    }
                    else if (pixel.Block && !includeBlocked)
                    {
                        continue;
                    }

                    _matrix[x, y].State = Metadata.PixelState.Default;
                    _matrix[x, y].Block = false;
                }
            }
        }
        private void RedrawGraphic()
        {
            for (var x = 0; x < xDimension; x++)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    var pixel = GetPixel(x, y);
                    if (pixel == null)
                    {
                        continue;
                    }
                    DrawPixel(x, y, pixel.State);
                }
            }
        }
        private PixelModel GetPixel(int x, int y)
        {
            if (CheckIfCoordinatesCorrect(x, y))
            {
                return _matrix[x, y];
            }

            return null;
        }

        private bool CheckIfCoordinatesCorrect(int x, int y)
        {
            if (x < 0 || y < 0 || x >= xDimension || y >= yDimension)
            {
                return false;
            }

            return true;
        }
        private void DrawPixel(int x, int y, Metadata.PixelState state)
        {
            var brush = Utils.GetColorBaseOnPixelState(state);
            _graphic.FillRectangle(brush, x, y, 1, 1);
        }
        private void ApplyPixelStateChange(int x, int y, bool blocked)
        {
            _matrix[x, y].Block = blocked;
        }
        private void ApplyPixelsStatesChange(IList<GrainModel> pixels)
        {
            foreach (var pixel in pixels)
            {
                ApplyPixelStateChange(pixel.X, pixel.Y, pixel.State, pixel.Block);
            }
        }
        private void ApplyPixelStateChange(int x, int y, Metadata.PixelState state)
        {
            _matrix[x, y].State = state;
            DrawPixel(x, y, state);
        }
        private void ApplyPixelStateChange(int x, int y, Metadata.PixelState state, bool blocked = false)
        {
            _matrix[x, y].Block = blocked;
            _matrix[x, y].State = state;
            DrawPixel(x, y, state);
        }

        private void CalculateBoarderPixels(bool includeInclusions = false)
        {
            _borderPixels.Clear();
            for (int x = 0; x < xDimension; x++)
            {
                for (int y = 0; y < yDimension; y++)
                {
                    for (int xN = -1; xN <= 1; xN++)
                    {
                        for (int yN = -1; yN <= 1; yN++)
                        {
                            var exist = CheckIfCoordinatesCorrect(x + xN, y + yN);
                            if (!exist)
                            {
                                continue;
                            }

                            var pixel1 = GetPixel(x, y);
                            var pixel2 = GetPixel(x + xN, y + yN);
                            if (includeInclusions)
                            {
                                if (pixel1.State == pixel2.State)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (pixel1.State == Metadata.PixelState.Inclusion || pixel2.State == Metadata.PixelState.Inclusion || pixel1.State == pixel2.State)
                                {
                                    continue;
                                }
                            }

                            _borderPixels.Add(new GrainModel()
                            {
                                X = x,
                                Y = y,
                                State = Metadata.PixelState.Border,
                                Block = true
                            });

                        }
                    }
                }
            }
        }









        //Algorithms
        private void SimpleGrainGrowth()
        {
            var newInfection = new List<GrainModel>();
            while (!_modelCalculated)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    for (var x = 0; x < xDimension; x++)
                    {
                        var pixel = GetPixel(x, y);

                        if (pixel == null || pixel.State != Metadata.PixelState.Default || pixel.Block)
                        {
                            continue;
                        }

                        var anyCellDifferentThanDefault = CheckIfNeighborhoodHasDifferentValue(x, y);

                        if (!anyCellDifferentThanDefault)
                        {
                            continue;
                        }

                        var state = GetMostFrequentValue(x, y);
                        if (state == null)
                        {
                            continue;
                        }

                        newInfection.Add(new GrainModel()
                        {
                            X = x,
                            Y = y,
                            State = state.Value
                        });
                        continue;
                    }
                }

                if (newInfection.Any())
                {
                    ApplyPixelsStatesChange(newInfection);
                    newInfection.Clear();
                }
                else
                {
                    _modelCalculated = true;
                }
            }
        }
        private void BoundaryShapeControl()
        {
            var newInfection = new List<GrainModel>();

            if (!int.TryParse(textBox4.Text, out var probability))
            {
                MessageBox.Show("Wrong Input", "x", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            while (!_modelCalculated)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    for (var x = 0; x < xDimension; x++)
                    {
                        if (_matrix[x, y].State != Metadata.PixelState.Default || _matrix[x, y].Block)
                        {
                            continue;
                        }

                        var role1 = CalculateRole1(x, y);
                        if (role1 != null)
                        {
                            newInfection.Add(new GrainModel()
                            {
                                X = x,
                                Y = y,
                                State = role1.Value
                            });
                            continue;
                        }

                        var role2 = CalculateRole2(x, y);
                        if (role2 != null)
                        {
                            newInfection.Add(new GrainModel()
                            {
                                X = x,
                                Y = y,
                                State = role2.Value
                            });
                            continue;
                        }

                        var role3 = CalculateRole3(x, y);
                        if (role3 != null)
                        {
                            newInfection.Add(new GrainModel()
                            {
                                X = x,
                                Y = y,
                                State = role3.Value
                            });
                            continue;
                        }

                        var role4 = CalculateRole4(x, y, probability);
                        if (role4 != null)
                        {
                            newInfection.Add(new GrainModel()
                            {
                                X = x,
                                Y = y,
                                State = role4.Value
                            });
                            continue;
                        }
                    }
                }

                if (newInfection.Any())
                {
                    ApplyPixelsStatesChange(newInfection);
                    newInfection.Clear();
                }
                else
                {
                    _modelCalculated = true;
                }
            }
        }
        private bool CheckIfNeighborhoodHasDifferentValue(int x, int y)
        {
            for (int yN = -1; yN <= 1; yN++)
            {
                for (int xN = -1; xN <= 1; xN++)
                {
                    if (xN == 0 && yN == 0)
                    {
                        continue;
                    }

                    var state = GetPixel(x - xN, y - yN);
                    if (state != null && state.State != Metadata.PixelState.Default)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        private Metadata.PixelState? GetMostFrequentValue(int x, int y)
        {
            var neighbours = new List<PixelModel>();
            for (var yN = -1; yN <= 1; yN++)
            {
                for (var xN = -1; xN <= 1; xN++)
                {
                    if (xN == 0 && yN == 0)
                    {
                        continue;
                    }

                    var value = GetPixel(x - xN, y - yN);
                    if (value == null || value.State == Metadata.PixelState.Default || value.State == Metadata.PixelState.Inclusion || value.Block)
                    {
                        continue;
                    }

                    neighbours.Add(new PixelModel()
                    {
                        State = value.State
                    });
                }
            }

            if (!neighbours.Any())
            {
                return null;
            }

            var result = neighbours.GroupBy(z => z.State).OrderByDescending(zz => zz.Count()).First();
            return result.Key;

        }
        private Metadata.PixelState? CalculateRole1(int x, int y)
        {
            var cellValues = new List<PixelModel>();
            for (var yN = -1; yN <= 1; yN++)
            {
                for (var xN = -1; xN <= 1; xN++)
                {
                    if (xN == 0 && yN == 0)
                    {
                        continue;
                    }

                    var value = GetPixel(x - xN, y - yN);
                    if (value == null || value.State == Metadata.PixelState.Default || value.State == Metadata.PixelState.Inclusion ||
                        value.Block)
                    {
                        continue;
                    }

                    cellValues.Add(new PixelModel()
                    {
                        State = value.State
                    });
                }
            }

            if (!cellValues.Any())
            {
                return null;
            }

            var result = cellValues.GroupBy(z => z.State).OrderByDescending(zz => zz.Count()).ToList();
            if (result.First().Count() >= 5)
            {
                return result.First().Key;
            }

            return null;
        }

        private Metadata.PixelState? CalculateRole2(int x, int y)
        {
            var cellValues = new List<PixelModel>();

            var value = GetPixel(x - 1, y);
            if (value != null && value.State != Metadata.PixelState.Default && value.State != Metadata.PixelState.Inclusion ||
                value != null && value.Block)
            {
                cellValues.Add(new PixelModel()
                {
                    State = value.State
                });
                ;
            }

            value = GetPixel(x, y - 1);
            if (value != null && value.State != Metadata.PixelState.Default && value.State != Metadata.PixelState.Inclusion ||
                value != null && value.Block)
            {
                cellValues.Add(new PixelModel()
                {
                    State = value.State
                });
                ;
            }

            value = GetPixel(x, y + 1);
            if (value != null && value.State != Metadata.PixelState.Default && value.State != Metadata.PixelState.Inclusion ||
                value != null && value.Block)
            {
                cellValues.Add(new PixelModel()
                {
                    State = value.State
                });
                ;
            }

            value = GetPixel(x + 1, y);
            if (value != null && value.State != Metadata.PixelState.Default && value.State != Metadata.PixelState.Inclusion ||
                value != null && value.Block)
            {
                cellValues.Add(new PixelModel()
                {
                    State = value.State
                });
                ;
            }

            if (!cellValues.Any())
            {
                return null;
            }

            var result = cellValues.GroupBy(z => z.State).OrderByDescending(zz => zz.Count()).ToList();
            if (result.First().Count() >= 3)
            {
                return result.First().Key;
            }

            return null;
        }

        private Metadata.PixelState? CalculateRole3(int x, int y)
        {
            var cellValues = new List<PixelModel>();

            var value = GetPixel(x - 1, y - 1);
            if (value != null && value.State != Metadata.PixelState.Default && value.State != Metadata.PixelState.Inclusion ||
                value != null && value.Block)
            {
                cellValues.Add(new PixelModel()
                {
                    State = value.State
                });
                ;
            }

            value = GetPixel(x - 1, y + 1);
            if (value != null && value.State != Metadata.PixelState.Default && value.State != Metadata.PixelState.Inclusion ||
                value != null && value.Block)
            {
                cellValues.Add(new PixelModel()
                {
                    State = value.State
                });
                ;
            }

            value = GetPixel(x + 1, y - 1);
            if (value != null && value.State != Metadata.PixelState.Default && value.State != Metadata.PixelState.Inclusion ||
                value != null && value.Block)
            {
                cellValues.Add(new PixelModel()
                {
                    State = value.State
                });
                ;
            }

            value = GetPixel(x + 1, y + 1);
            if (value != null && value.State != Metadata.PixelState.Default && value.State != Metadata.PixelState.Inclusion ||
                value != null && value.Block)
            {
                cellValues.Add(new PixelModel()
                {
                    State = value.State
                });
                ;
            }

            if (!cellValues.Any())
            {
                return null;
            }

            var result = cellValues.GroupBy(z => z.State).OrderByDescending(zz => zz.Count()).ToList();
            if (result.First().Count() >= 3)
            {
                return result.First().Key;
            }

            return null;
        }

        private Metadata.PixelState? CalculateRole4(int x, int y, int probability)
        {
            var random = rand.Next(1, 100);
            if (random > probability)
            {
                return null;
            }

            var result = GetMostFrequentValue(x, y);
            return result;
        }
    }
}
