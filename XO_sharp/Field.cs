using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms.VisualStyles;

namespace XO_sharp
{
    public class Field
    {
        public class cell
        {
            private int status;//статус X или O
            private double mark;//оценка нужности хода в данную клетку
            public int getStatus()
            {
                return this.status;
            }
            public void setStatus(int newStatus)
            {
                this.status = newStatus;
                this.mark = 0;
            }
            public void setMark(double mark)
            {
                this.mark = mark;
            }
            public double getMark()
            {
                return this.mark;
            }

            public void clear()
            {
                this.setStatus(0);
                this.setMark(0);
            }
        };

        public int CountStep = 0;//счетчик ходов
        public int Win = -1;//кто выиграл
        public cell[][] Cells;//само поле
        public LinkedList<Point> WinCells;
        private LinkedList<Point> Steps;
        private static int _sizePlayground = 19;//размер поля
        private static double K = 1;//коэффициент определяющий стратегию игры (> 1 - защита), (< 1 - нападение)
        private static int _criticalLength = 5;//длина выигрышной комбинации
        private int _curStatus;//чей текущий ход X или O

        //для построения дерева
        private class NodeMapSteps
        {
            public int Win = -1;
            private List<NodeMapSteps> _listSteps = new List<NodeMapSteps>();
        }
        private static List<Point> _historyStep = new List<Point>();
        private static int DeepMap = 0;
        private const int MaxDeepMap = 3;
        private List<Point> ListMaxSteps = new List<Point>();
        
        private void CreateMap()
        {
            while (DeepMap < MaxDeepMap)
            {
                
            }
        }
        
        public void SetCurStatus(int status)
        {
            this._curStatus = status;
        }
        public int GetCurStatus()
        {
            return this._curStatus;
        }
        
        public static double Factorial(double l)
        {
            if (l <= 1) return 1;
            return l * Factorial(l - 1);
        }

        public Point LastStep()
        {
            return Steps.Last.Value;
        }

        private cell[][] Create()
        {
            cell[][] cells = new cell[_sizePlayground][];
            for (int i = 0; i < _sizePlayground; i++)
            {
                cells[i] = new cell[_sizePlayground];
                for (int j = 0; j < _sizePlayground; j++)
                {
                    cells[i][j] = new cell();
                    cells[i][j].clear();
                }
            }
            return cells;
        }

        public void ClearField()
        {
            for (int i = 0; i < _sizePlayground; i++)
                for (int j = 0; j < _sizePlayground; j++)
                    Cells[i][j].clear();
        }

        public void SetBeginParams()
        {
            Win = -1;
            CountStep = 0;
            SetCurStatus(2);
        }
        
        public Field(int size, int length)
        {
            _criticalLength = length;
            _sizePlayground = size;
            SetBeginParams();
            Cells = Create();
            Steps = new LinkedList<Point>();
        }
        //проверить можно ли поставить фишку в указанную ячейку
        private bool CheckValidRowCol(int row, int col)
        {
	        return row >= 0 && row<_sizePlayground && col >= 0 && col<_sizePlayground;
        }
        //проверить есть ли рядом занятые ячейки
        private bool NearbyExistCell(int row, int col)
        {
	        bool nearby;
            int r = row, c = col;
            int sw = -1;
	        while (CheckValidRowCol(r, c) && Cells[r][c].getStatus() == 0 && ++sw< 8)
	        {
		        switch (sw)
		        {
		        case 0: {r--; c--; break; }
		        case 1: {c++; break; }
		        case 2: {c++; break; }
		        case 3: {r++; break; }
		        case 4: {r++; break; }
		        case 5: {c--; break; }
		        case 6: {c--; break; }
		        case 7: {r--; break; }
		        }
		        while (!CheckValidRowCol(r, c) && ++sw<8)
			        switch (sw)
			        {
			        case 0: {r--; c--; break; }
			        case 1: {c++; break; }
			        case 2: {c++; break; }
			        case 3: {r++; break; }
			        case 4: {r++; break; }
			        case 5: {c--; break; }
			        case 6: {c--; break; }
			        case 7: {r--; break; }
			        }
	        }
	        if (CheckValidRowCol(r, c) && Cells[r][c].getStatus() != 0) nearby = true;
	        else nearby = false;
	        return nearby;
        }
        //высчитать оценку на основе самой длинной возможной последовательности
        private double CalculateMarkForHVRL(bool alien, double length, int criticalLength)
        {
            double mark;
            if (length < 1.1) mark = 1;
            /*если ход приводит к нашему выигрышу, то рейтинг должен быть выше, чем выигрыш соперника*/
            else if (!alien && length >= criticalLength - 0.1) mark = Double.PositiveInfinity;
            /*если ход приводит к выигрышу соперника, то рейтинг должен быть самым высоким*/
            else if (alien && length >= criticalLength - 0.1) mark = 2 * Math.Pow(length - 1, 5);
            /*последовательность должна иметь рейтинг выше 4х последовательностей длинной на единицу меньше,
            т.е. нужно прибавить 3, т.к. 3!=6*/
            else mark = 2 * Math.Pow(length - 1, 4);
            return mark;
        }
        //получить список ячеек от конкретной ячейки сверху вниз
        private List<int> GetListCeilsV(int rowIn, int colIn, ref int place)
        {
            List<int> massive = new List<int>(0);
            int minRow = Math.Max(0, rowIn - _criticalLength),
                maxRow = Math.Min(_sizePlayground - 1, rowIn + _criticalLength),
                minCol = colIn,
                maxCol = colIn;
            place = rowIn - minRow;
            for (int i = minRow, j = minCol; i <= maxRow && j <= maxCol; i++)
                massive.Add(Cells[i][j].getStatus());
            return massive;
        }
        //получить список ячеек от конкретной ячейки по горизонтали слева направо
        private List<int> GetListCeilsH(int rowIn, int colIn, ref int place)
        {
            List<int> massive = new List<int>(0);
            int minRow = rowIn,
                maxRow = rowIn,
                minCol = Math.Max(0, colIn - _criticalLength),
                maxCol = Math.Min(_sizePlayground - 1, colIn + _criticalLength);
            place = colIn - minCol;
            for (int i = minRow, j = minCol; i <= maxRow && j <= maxCol; j++)
                massive.Add(Cells[i][j].getStatus());
            return massive;
        }
        //получить список ячеек от конкретной ячейки по диагонали от верхнего правого до нижнего левого
        private List<int> GetListCeilsR(int rowIn, int colIn, ref int place)
        {
            List<int> massive = new List<int>(0);
            int minRow = rowIn,
                maxRow = rowIn,
                minCol = colIn,
                maxCol = colIn;
            int counter = 0;
            place = 0;
            while (CheckValidRowCol(--minRow, ++minCol) && place < _criticalLength) ++place;
            ++minRow; --minCol;
            while (CheckValidRowCol(++maxRow, --maxCol) && counter < _criticalLength) ++counter;
            --maxRow; ++maxCol;
            for (int i = minRow, j = minCol; i <= maxRow && j >= maxCol; ++i, --j)
                massive.Add(Cells[i][j].getStatus());
            return massive;
        }
        //получить список ячеек от конкретной ячейки по диагонали от верхнего левого до нижнего правого
        private List<int> GetListCeilsL(int rowIn, int colIn, ref int place)
        {
            List<int> massive = new List<int>(0);
            int minRow = rowIn,
                maxRow = rowIn,
                minCol = colIn,
                maxCol = colIn;
            int counter = 0;
            place = 0;
            while (CheckValidRowCol(--minRow, --minCol) && place < _criticalLength) ++place;
            ++minRow; ++minCol;
            while (CheckValidRowCol(++maxRow, ++maxCol) && counter < _criticalLength) ++counter;
            --maxRow; --maxCol;
            for (int i = minRow, j = minCol; i <= maxRow && j <= maxCol; ++i, ++j)
                massive.Add(Cells[i][j].getStatus());
            return massive;
        }
        //максимальная последовательность одинаковых фишек во входном списке
        private double MaxSequenceEqualsElement(List<int> massive, ref int place, int criticalLength, int status)
        {
            double k0 = 0,
                k1 = 0;
            //флаг, определяющий возможность серии длины _criticalLength
            bool maybe = false;
            int iter = -1;
            //опеределяем возможна серия из своих из своих фишек
            if (massive.Count >= criticalLength)
            {
                while (++iter < massive.Count && !maybe)
                {
                    bool findAlien = false;
                    int iterInner = iter;
                    var counter = 0;
                    //пока не дойдем до конца или не встретим чужую фишку
                    while (++iterInner < massive.Count && 
                        !findAlien && 
                        !maybe)
                    {
                        if (massive[iterInner] == status % 2 + 1)
                            findAlien = true;
                        else if (++counter >= criticalLength)
                            maybe = true;
                    }
                }
            }
            if (maybe)
            {
                int ind, nvl;

                // |*^*|
                k0 = 1;
                nvl = 0;
                ind = place;
                while (--ind >= 0 && massive[ind] == status)
                    ++k0;
                ind = place;
                while (++ind < massive.Count && massive[ind] == status)
                    ++k0;

                // |_*^*_|
                k1 = 1;
                nvl = 0;
                ind = place;
                while (--ind >= 0 && massive[ind] != status % 2 + 1 && nvl == 0)
                {
                    if (massive[ind] == 0) nvl = 1;
                    else if (massive[ind] == status) ++k1;
                }
                ind = place;
                while (++ind < massive.Count && massive[ind] != status % 2 + 1 && nvl <= 1)
                {
                    if (massive[ind] == 0 && nvl == 0) nvl = 3;
                    else if (massive[ind] == 0 && nvl == 1) nvl = 2;
                    else if (massive[ind] == status) ++k1;
                }
                //если по концам последовательности есть пустые клетки, то nvl == 2, 
                //если только слева то 1, 
                //если только справа то 3
                if (nvl == 2 && k1 >= criticalLength - 1) k1 += 0.2;
                else if (nvl == 2 && k1 >= criticalLength - 2) k1 += 0.1;
            }
            return Math.Max(Math.Max(k0, k1), 0);
        }
        //высчитать оценку для ячейки для конкретного игрока
        private double CalculateMarkForCeil(int row, int col, bool alien)
        {
	        int place = 0;
            //по четырем сторонам
            int threeThrees = 0;
            double v = MaxSequenceEqualsElement(GetListCeilsV(row, col, ref place), ref place, _criticalLength, GetCurStatus());
            if (Math.Abs((v - (int) v)*10 - 1) < 0.00001) ++threeThrees;
	        double h = MaxSequenceEqualsElement(GetListCeilsH(row, col, ref place), ref place, _criticalLength, GetCurStatus());
            if (Math.Abs((h - (int) h)*10 - 1) < 0.00001) ++threeThrees;
            double r = MaxSequenceEqualsElement(GetListCeilsR(row, col, ref place), ref place, _criticalLength, GetCurStatus());
            if (Math.Abs((r - (int) r)*10 - 1) < 0.00001) ++threeThrees;
            double l = MaxSequenceEqualsElement(GetListCeilsL(row, col, ref place), ref place, _criticalLength, GetCurStatus());
            if (Math.Abs((l - (int) l)*10 - 1) < 0.00001) ++threeThrees;
            if (threeThrees >= 2)
            {
                if (Math.Abs((v - (int) v)*10 - 1) < 0.00001) v += 2;
                if (Math.Abs((h - (int) h)*10 - 1) < 0.00001) h += 2;
                if (Math.Abs((r - (int) r)*10 - 1) < 0.00001) r += 2;
                if (Math.Abs((l - (int) l)*10 - 1) < 0.00001) l += 2;
            }
            double result = CalculateMarkForHVRL(alien, v, _criticalLength);
            result += CalculateMarkForHVRL(alien, h, _criticalLength);
            result += CalculateMarkForHVRL(alien, r, _criticalLength);
            result += CalculateMarkForHVRL(alien, l, _criticalLength);
            result /= 4;
            //result = Math.Sqrt(result);
            return result;
        }
        //поменять текущий ход
        private void ChangeCurStatus()
        {
            SetCurStatus(GetCurStatus() % 2 + 1);
        }
        //получаем итоговую оценку для каждой ячейки
        private double GetMark(int row, int col)
        {
            double F = 0;
            if (Cells[row][col].getStatus() == 0)// && nearbyExistCell(row, col))
            {
                ChangeCurStatus(); //меняем статус для своего хода
                Cells[row][col].setStatus(GetCurStatus());
                double M = CalculateMarkForCeil(row, col, false);

                ChangeCurStatus(); //меняем статус для чужого хода
                Cells[row][col].setStatus(GetCurStatus());
                double Q = CalculateMarkForCeil(row, col, true);

                Cells[row][col].setStatus(0); //обнуляем статус
                F = M + Q * K;
            }
            return F;
        }
        //выставить оценки хода для каждой ячейки
        public void SetMarks()
        {
            for (int i = 0; i < _sizePlayground; i++)
                for (int j = 0; j < _sizePlayground; j++)
                    Cells[i][j].setMark(GetMark(i, j));
        }
        //узнать Point c максимальной оценкой
        private Point GetPointWithMaxMark()
        {
	        double maxMarkValue = Double.MinValue;
            List<Point> points = new List<Point>(0),
                        pointsNvl = new List<Point>(0);
	        for (int i = 0; i < _sizePlayground; ++i)
	        {
		        for (int j = 0; j < _sizePlayground; ++j)
		        {
			        double mark = Cells[i][j].getMark();
			        if (Cells[i][j].getStatus() == 0 && Math.Abs(mark) > 0.01)
			        {
				        if (maxMarkValue<mark)
				        {
					        maxMarkValue = mark;
					        points.Clear();
					        points.Add(new Point(i, j));
				        }
				        else if (Math.Abs(maxMarkValue - mark) < 0.01 && points.Count!=0)
					        points.Add(new Point(i, j));
			        }
			        else if (Cells[i][j].getStatus() == 0)
				        pointsNvl.Add(new Point(i, j));
		        }
	        }
	        Point result;
	        switch (points.Count)
	        {
	            case 0:
	                result = pointsNvl[pointsNvl.Count / 2];
	                break;
	            default:
	            {
	                for (int i = 0; i < Math.Min(3, points.Count); ++i)
	                    ListMaxSteps.Add(points[i]);
	                result = points[0];//points.Count / 2];
                    break;
	            }
	        }
	        return result;
        }
        //узнать Point, куда лучше поставить фишку
        public Point Step()
        {
            Point point = GetPointWithMaxMark();
            return point;
        }
        
        //сделать ход в указаную клетку
        public void Step(Point point)
        {
            SetCurStatus(GetCurStatus() % 2 + 1);
            Cells[point.X][point.Y].setStatus(GetCurStatus());
            ++CountStep;
            Steps.AddLast(point);
        }

        //отменить последний ход
        public Point ReverseStep()
        {
            Point lastStep = this.LastStep();
            Cells[lastStep.X][lastStep.Y].clear();
            --CountStep;
            Steps.RemoveLast();
            return lastStep;
        }

        //проверить есть ли во входном массиве выигравшая комбинация, и возвращает начало этой комбинации
        private bool CheckSequences(List<int> massive, int length, ref int begin)
        {
            //massive = new List<int>(19) {0, 1, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
            bool gameOver = false;
            int counterEqualsElments = 1;
            int status = 0;
            int i = 0;
            while (i < massive.Count && !gameOver)
            {
                status = massive[i];
                if (status != 0)
                {
                    for (int j = i+1; j < massive.Count && massive[j] == status && counterEqualsElments < _criticalLength; ++j)
                        ++counterEqualsElments;
                    if (counterEqualsElments >= length)
                    {
                        begin = i;
                        gameOver = true;
                    }
                    else
                        counterEqualsElments = 1;
                }
                ++i;
            }
            if (gameOver)
                Win = status;
            return gameOver;
        }
        
        //проверить есть ли выигравший игрок, и если да до записать выигравшую комбинацию в WinCells
        public void CheckEndGame()
        {
            bool end = false;
            int beginPosition = 0;
            List<int> massive = new List<int>(0);
            /* | 
            j-const*/ 
            for (int j = 0; j < _sizePlayground && !end; j++)
            {
                massive.Clear();
                for (int i = 0; i < _sizePlayground; i++)
                    massive.Add(Cells[i][j].getStatus());
                end = CheckSequences(massive, _criticalLength, ref beginPosition);
                if (end)
                {
                    WinCells = new LinkedList<Point>();
                    for (int i = beginPosition;
                        i < beginPosition + _criticalLength;
                        ++i)
                        WinCells.AddLast(new Point(x: i, y: j));
                }
            }
            /* - 
            i-const*/
            for (int i = 0; i < _sizePlayground && !end; i++)
            {
                massive.Clear();
                for (int j = 0; j < _sizePlayground; j++)
                    massive.Add(Cells[i][j].getStatus());
                end = CheckSequences(massive, _criticalLength, ref beginPosition);
                if (end)
                {
                    WinCells = new LinkedList<Point>();
                    for (int j = beginPosition; 
                        j < beginPosition + _criticalLength; 
                        ++j)
                        WinCells.AddLast(new Point(x: i, y: j));
                }
            }
            /* / 
            сначала идем по левой стороне и по диагонали вправо-вверх
            потом снизу и по диагонали вправо-вверх*/
            for (int i = 0, j = 0; i < _sizePlayground && !end; ++i)
            {
                massive.Clear();
                int min_row = i,
                    max_row = i,
                    min_col = j,
                    max_col = j;
                int counter = 1;
                //вверх вправо
                while (CheckValidRowCol(--min_row, ++max_col)) ++counter;
                ++min_row; --max_col;
                if (counter >= _criticalLength)
                {
                    for (int k = max_row, l = min_col; 
                        k >= min_row && l <= max_col; 
                        --k, ++l)
                        massive.Add(Cells[k][l].getStatus());
                    end = CheckSequences(massive, _criticalLength, ref beginPosition);
                    if (end)
                    {
                        WinCells = new LinkedList<Point>();
                        for (int k = max_row - beginPosition, l = min_col + beginPosition; 
                            k > max_row - beginPosition - _criticalLength && l < min_col + beginPosition + _criticalLength; 
                            --k, ++l)
                            WinCells.AddLast(new Point(x: k, y: l));
                    }
                }
                else end = false;
            }
            for (int i = _sizePlayground - 1, j = 0; j < _sizePlayground && !end; ++j)
            {
                massive.Clear();
                int min_row = i,
                    max_row = i,
                    min_col = j,
                    max_col = j;
                int counter = 1;
                //вверх вправо
                while (CheckValidRowCol(--min_row, ++max_col)) ++counter;
                ++min_row; --max_col;
                if (counter >= _criticalLength)
                {
                    for (int k = max_row, l = min_col; 
                        k >= min_row && l <= max_col; 
                        --k, ++l)
                        massive.Add(Cells[k][l].getStatus());
                    end = CheckSequences(massive, _criticalLength, ref beginPosition);
                    if (end)
                    {
                        WinCells = new LinkedList<Point>();
                        for (int k = max_row - beginPosition, l = min_col + beginPosition;
                            k > max_row - beginPosition - _criticalLength && l < min_col + beginPosition + _criticalLength;
                            --k, ++l)
                            WinCells.AddLast(new Point(x: k, y: l));
                    }
                }
                else end = false;
            }
            /* \ */
            for (int i = _sizePlayground - 1, j = 0; i >= 0 && !end; --i)
            {
                massive.Clear();
                int min_row = i,
                    max_row = i,
                    min_col = j,
                    max_col = j;
                int counter = 1;
                /*вниз вправо*/
                while (CheckValidRowCol(++max_row, ++max_col)) ++counter;
                --max_row; --max_col;
                if (counter >= _criticalLength)
                {
                    for (int k = max_row, l = max_col; 
                        k >= min_row && l >= min_col; 
                        --k, --l)
                        massive.Add(Cells[k][l].getStatus());
                    end = CheckSequences(massive, _criticalLength, ref beginPosition);
                    if (end)
                    {
                        WinCells = new LinkedList<Point>();
                        for (int k = max_row - beginPosition, l = max_col - beginPosition;
                            k > max_row - beginPosition - _criticalLength && l > max_col - beginPosition - _criticalLength;
                            --k, --l)
                            WinCells.AddLast(new Point(x: k, y: l));
                    }
                }
                else end = false;
            }
            for (int i = 0, j = 0; j < _sizePlayground && !end; ++j)
            {
                massive.Clear();
                int min_row = i,
                    max_row = i,
                    min_col = j,
                    max_col = j;
                int counter = 1;
                /*вниз вправо*/
                while (CheckValidRowCol(++max_row, ++max_col)) ++counter;
                --max_row; --max_col;
                if (counter >= _criticalLength)
                {
                    for (int k = max_row, l = max_col; 
                        k >= min_row && l >= min_col;
                        --k, --l)
                        massive.Add(Cells[k][l].getStatus());
                    end = CheckSequences(massive, _criticalLength, ref beginPosition);
                    if (end)
                    {
                        WinCells = new LinkedList<Point>();
                        for (int k = max_row - beginPosition, l = max_col - beginPosition;
                            k > max_row - beginPosition - _criticalLength && l > max_col - beginPosition - _criticalLength;
                            --k, --l)
                            WinCells.AddLast(new Point(x: k, y: l));
                    }
                }
                else end = false;
            }
            if (!end && CountStep == _sizePlayground*_sizePlayground)
            {
                Win = 0;
                end = true;
            }
        }

    }
}