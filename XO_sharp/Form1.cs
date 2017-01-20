using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace XO_sharp
{
    public partial class Form1 : Form
    {
        private const int SizePlayground = 19;
        private const int CriticalLength = 5;
        private static readonly int _ClientSize = Screen.PrimaryScreen.WorkingArea.Height - 100;
        private static readonly int SizeCell = _ClientSize/SizePlayground;
        private TableLayoutPanel _tableLayoutPanel;
        private Button[][] _buttons;
        private Field _field;
        private Point _hintLastStep;
        private string _typeGame = "PC vs Human";
        public Form1()
        {
            InitializeComponent();
            CreatePlayground();
        }

        private void CreatePlayground()
        {
            _tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            _tableLayoutPanel.SuspendLayout();
            _buttons = new Button[SizePlayground][];
            _field = new Field(SizePlayground,CriticalLength);

            _tableLayoutPanel.ColumnCount = SizePlayground;
            _tableLayoutPanel.RowCount = SizePlayground;
            for (int i = 0; i < _tableLayoutPanel.RowCount; i++)
            {
                _tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, SizeCell));
                _buttons[i] = new Button[SizePlayground];
                for (int j = 0; j < _tableLayoutPanel.ColumnCount; j++)
                {
                    _tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SizeCell));
                    _buttons[i][j] = new Button
                    {
                        Dock = DockStyle.Fill,
                        Enabled = true,
                        BackColor = Color.Snow,
                        TabStop = false,
                    };
                    _buttons[i][j].Font = new Font("Microsoft Sans Serif", _buttons[i][j].Height * 7 / 12);
                    _buttons[i][j].Click += new EventHandler(button_Click);
                    _tableLayoutPanel.Controls.Add(_buttons[i][j], j, i);
                }
            }
            _tableLayoutPanel.Location = new Point(0, HintStepToolStripMenuItem.Height*3/2);
            _tableLayoutPanel.Size = new Size(SizePlayground * SizeCell, SizePlayground * SizeCell);
            ClientSize = new Size(_tableLayoutPanel.Width, HintStepToolStripMenuItem.Height*3/2+_tableLayoutPanel.Height+StatusMark.Height*3/2);
            Controls.Add(_tableLayoutPanel);
            _tableLayoutPanel.ResumeLayout();
            _field.SetCurStatus(2);
            StatusMark.Text = "";
            timerPCvsPC.Interval = 100;
        }

        private void PrintMarks()
        {
            for (int i = 0; i < SizePlayground; i++)
                for (int j = 0; j < SizePlayground; j++)
                { 
                    double mark = _field.Cells[i][j].getMark();
                    if (_buttons[i][j].Enabled)
                        _buttons[i][j].Text = Math.Round(mark,0).ToString(CultureInfo.CurrentCulture);
                }
        }

        private void Restart(string tmpTypeGame)
        {
            ReDrawField();
            _typeGame = tmpTypeGame;
            if (_typeGame == "PC vs PC")
                timerPCvsPC.Start();
        }

        private void ReDrawField()
        {
            _tableLayoutPanel.SuspendLayout();
            for (int i = 0; i < SizePlayground; i++)
            {
                for (int j = 0; j < SizePlayground; j++)
                {
                    Button b = _buttons[i][j];
                    if (!b.Enabled || b.BackColor!=Color.Snow || b.Text!="")
                        ClearButton(b);
                }
            }
            _tableLayoutPanel.ResumeLayout();
            _field.ClearField();
            _field.SetBeginParams();
            StatusMark.Text = "";
        }

        private void ClearButton(Button b)
        {
            b.SuspendLayout();
            b.BackColor = Color.Snow;
            b.Text = "";
            b.Enabled = true;
            b.ResumeLayout();
        }

        private void CancelHintStep()
        {
            Button b = _buttons[_hintLastStep.X][_hintLastStep.Y];
            if (b.BackColor == Color.Red)
                ClearButton(b);
        }

        private Button TempStep(Point point, Color color, bool enabled)
        {
            if (/*_field.Steps.Count*/ _field.CountStep != 0 && !_buttons[_field.LastStep().X][_field.LastStep().Y].Enabled)
                    _buttons[_field.LastStep().X][_field.LastStep().Y].BackColor = Color.AliceBlue;
            //StatusMark.Text = _field.Cells[point.X][point.Y].getMark().ToString(CultureInfo.CurrentCulture);
            int status = _field.GetCurStatus() % 2 + 1;
            Button b = _buttons[point.X][point.Y];
            b.SuspendLayout();
            b.ForeColor = (status == 1 ? Color.DarkGreen : Color.Blue);
            b.Text = (status == 1 ? "X" : "O");
            b.BackColor = color;
            b.Enabled = enabled;
            b.ResumeLayout();
            return b;
        }

        private void HintStep(Point p)
        {
            Button b = TempStep(p, Color.Red, true);
            _hintLastStep = p;
        }

        private void Step(Point p)
        {
            CancelHintStep();
            Button b = TempStep(p, Color.Aqua, false);
            _field.Step(p);
            CheckEndGame();
        }

        private void StepHuman(object sender)
        {
            Button b = (Button)sender;
            for (int i = 0; i < SizePlayground; i++)
                for (int j = 0; j < SizePlayground; j++)
                    if (b.Equals(_buttons[i][j]))
                        Step(new Point(i, j));
        }

        private void StepBot()
        {
            _field.SetMarks();
            //PrintMarks();
            Step(_field.Step());
        }

        private void DispWinSeq()
        {
            LinkedList<Point>.Enumerator en = _field.WinCells.GetEnumerator();
            while (en.MoveNext())
                _buttons[en.Current.X][en.Current.Y].BackColor = Color.Coral;
        }

        private void CheckEndGame()
        {
            _field.CheckEndGame();
            string message;
            if (_field.Win > 0)
            {
                message = "Выиграли " + (_field.Win == 1 ? "X" : "O") + "!";
                DispWinSeq();
            }
            else if (_field.Win == 0)
                message = "Ничья!";
            else
                message = "";
            if (message != "")
            {
                timerPCvsPC.Stop();
                if (MessageBox.Show(message + "\nХотите начать новую игру?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    Restart(_typeGame);
            }
        }
        
        private void button_Click(object sender, EventArgs e)
        {
            if (_field.Win == -1 &&
                _typeGame.Contains("Human"))
            {
                StepHuman(sender);
                if (_field.Win == -1 && 
                    _typeGame.Contains("PC"))
                    StepBot();
            }
        }

        private void hintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_field.Win == -1)
                HintStep(_field.Step());
        }

        private void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timerPCvsPC.Stop();
            if (_field.CountStep == 0 || (_field.CountStep !=0 &&
                MessageBox.Show("\nХотите завершить текущую игру и начать новую?", "", MessageBoxButtons.YesNo) ==
                DialogResult.Yes))
                Restart((sender as ToolStripMenuItem).Text);
        }

        private void ReverseStep()
        {
            if (_typeGame.Contains("Human"))
            {
                Point lastStep;
                lastStep = _field.ReverseStep();
                ClearButton(_buttons[lastStep.X][lastStep.Y]);
                if (_typeGame.Contains("PC"))
                {
                    lastStep = _field.ReverseStep();
                    ClearButton(_buttons[lastStep.X][lastStep.Y]);
                }
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (_typeGame == "PC vs PC")
                timerPCvsPC.Enabled = !timerPCvsPC.Enabled;
            else if (e.Control && e.KeyCode == Keys.Z)
                ReverseStep();
            else if (e.Control && e.KeyCode == Keys.Space)
                hintToolStripMenuItem_Click(sender,e);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            StepBot();
        }
    }
}
