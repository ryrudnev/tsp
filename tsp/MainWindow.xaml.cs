using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace tsp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Текущий орг. граф 
        private static Digraph graph = new Digraph();

        // Текуший маршрут коммивояжера
        private static Digraph.Path pathTs = new Digraph.Path(graph);

        // Трассировочный метод ветвей и границ
        private static BranchAndBound iterator = new BranchAndBound(graph);

        #region Управление скроллом просмотра изображения

        private Point? lastCenterPositionOnTarget;

        private Point? lastMousePositionOnTarget;

        private Point? lastDragPoint;

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (lastDragPoint.HasValue)
            {
                Point posNow = e.GetPosition(scrollViewer);

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - dX);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - dY);
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(scrollViewer);
            if (mousePos.X <= scrollViewer.ViewportWidth && mousePos.Y < scrollViewer.ViewportHeight) //make sure we still can use the scrollbars
            {
                scrollViewer.Cursor = Cursors.SizeAll;
                lastDragPoint = mousePos;
                Mouse.Capture(scrollViewer);
            }
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            lastMousePositionOnTarget = Mouse.GetPosition(grid);

            if (e.Delta > 0)
            {
                slider.Value += 1;
            }
            if (e.Delta < 0)
            {
                slider.Value -= 1;
            }

            e.Handled = true;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            scrollViewer.Cursor = Cursors.Arrow;
            scrollViewer.ReleaseMouseCapture();
            lastDragPoint = null;
        }

        private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scaleTransform.ScaleX = e.NewValue;
            scaleTransform.ScaleY = e.NewValue;

            var centerOfViewport = new Point(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = scrollViewer.TranslatePoint(centerOfViewport, grid);
        }

        private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                Point? targetBefore = null;
                Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue)
                {
                    if (lastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new Point(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
                        Point centerOfTargetNow = scrollViewer.TranslatePoint(centerOfViewport, grid);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                }
                else
                {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(grid);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue)
                {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    double multiplicatorX = e.ExtentWidth / grid.Width;
                    double multiplicatorY = e.ExtentHeight / grid.Height;

                    double newOffsetX = scrollViewer.HorizontalOffset - dXInTargetPixels * multiplicatorX;
                    double newOffsetY = scrollViewer.VerticalOffset - dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                    {
                        return;
                    }

                    scrollViewer.ScrollToHorizontalOffset(newOffsetX);
                    scrollViewer.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }

        #endregion

        #region Фоновое исполнение

        // окно ожидания выполнения метода ветвей и границ
        WaitingWindow wait;

        // фоновый исполнитель
        private BackgroundWorker tspBackgroundWoker;

        // выполнение метода ветвей и границ
        private void TspDoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            BackgroundWorker tspWorker = sender as BackgroundWorker;
            tspWorker.ReportProgress(0);

            pathTs = BranchAndBound.Tsp(graph);
        }

        // отображенеи измения прогресса выполнения метода ветвей и границ
        private void TspProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            wait = new WaitingWindow();
            wait.ShowDialog();
        }

        // окончание выполнение метода ветвей и границ
        private void TspRunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            wait.Close();

            var vertexInTsPath = pathTs.GetVertexInOrderTraversal();
            if (vertexInTsPath.Count == 0)
            {
                MessageBox.Show("Маршрут коммивояжера не найден.", "Поиск маршрута завершен!", MessageBoxButton.OK, MessageBoxImage.Information);
                tspPathTxtBox.Text = "Маршрута коммивояжера для данного графа не существует.";
                return;
            }

            // отрисовка графа с выделенным путем
            try
            {
                tspImg.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Painter.Drawing(graph, pathTs).GetHbitmap(),
                    IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // составление маршрута коммивояжера
            tspPathTxtBox.Text = "Стоимость маршрута: " + pathTs.Cost + "\n";
            tspPathTxtBox.Text += "\nМаршрут: ";

            for (int i = 0; i < vertexInTsPath.Count; i++)
            {
                tspPathTxtBox.Text += vertexInTsPath[i] + 1;
                if (i != vertexInTsPath.Count - 1)
                    tspPathTxtBox.Text += " -> ";
            }

            MessageBox.Show("Маршрут коммивояжера найден.", "Поиск маршрута завершен!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion 

        public MainWindow()
        {
            InitializeComponent();

            tspBackgroundWoker = (BackgroundWorker)this.FindResource("tspBackgroundWoker");

            scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            scrollViewer.MouseLeftButtonUp += OnMouseLeftButtonUp;
            scrollViewer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
            scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;

            scrollViewer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            scrollViewer.MouseMove += OnMouseMove;

            slider.ValueChanged += OnSliderValueChanged;
        }
        

        // открытие файла с матрицей смежности 
        private void FileOpen(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.AddExtension = true;
            dialog.Filter = "Текстовые документы |*.txt";

            if (dialog.ShowDialog() == true)
                inputGraphTxtBox.Text = File.ReadAllText(dialog.FileName);
        }

        // задание графа
        private void EnterGraph(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(inputGraphTxtBox.Text))
            {
                MessageBox.Show("Пустое текстовое поле.", "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var lines = inputGraphTxtBox.Text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 30)
            {
                MessageBox.Show("Недопустимый размер матрицы смежности. Максимальный рамзер - 30.", "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var matrix = new Digraph.AdjacencyMatrix(lines.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                var values = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length != lines.Length)
                {
                    MessageBox.Show("Матрица смежности должна быть квадратной.", "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                for (int j = 0; j < lines.Length; j++)
                {
                    float c;

                    if (!float.TryParse(values[j], out c))
                    {
                        if (values[j].CompareTo("*") == 0)
                            matrix[i, j] = float.PositiveInfinity;
                        else
                        {
                            MessageBox.Show("Матрица смежности содержит недопустимые символы.", "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    else if (c < 0)
                    {
                        MessageBox.Show("Матрица смежности содержит отрицательные элементы", "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    else if (i == j)
                    {
                        MessageBox.Show("Матрица смежности должна описывать полный граф.", "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    else
                        matrix[i, j] = c;
                }
            }

            // изменение матрицы графа
            graph.Adjacency = matrix;

            // новый метод ветвей и границ для трассировки
            iterator = new BranchAndBound(graph);

            // установка изображения графа
            try
            {
                tspImg.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Painter.Drawing(graph).GetHbitmap(),
                    IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // обнуление информации о маршруте
            tspPathTxtBox.Text = "";
        }

        // просмотр изображения графа
        private void ShowGraph(object sender, RoutedEventArgs e)
        {
            var window = new Window();
            window.Title = "Визуализация графа";

            Image img = new Image();
            try
            {
                img.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Painter.Drawing(graph).GetHbitmap(),
                    IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            window.Content = img;

            window.ShowDialog();
        }

        // запуск метода ветвей и границ
        private void RunTsp(object sender, RoutedEventArgs e)
        {
            if (graph.CountVertex() == 0)
            {
                MessageBox.Show("Невозможно найти маршрут коммиворяжера.\nГраф не задан.", "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // обнуление режима трассировки метода ветвей и границ
            traceTspBttn.Content = "Трассировать ветвление метода";
            stepBackTspBttn.IsEnabled = false;
            stepForwardTspBttn.IsEnabled = false;

            // фоновое выполнение метода ветвей и границ 
            tspBackgroundWoker.RunWorkerAsync();
        }

        #region Трассировочный метод ветвей и границ

        // запуск трассировочного метода ветвей и границ
        private void TraceTsp(object sender, RoutedEventArgs e)
        {
            if (traceTspBttn.Content as string == "Трассировать ветвление метода")
            {
                traceTspBttn.Content = "Выйти из трассировки";
                stepBackTspBttn.IsEnabled = true;
                stepForwardTspBttn.IsEnabled = true;
                iterator.Reset();
            }
            else
            {
                traceTspBttn.Content = "Трассировать ветвление метода";
                stepBackTspBttn.IsEnabled = false;
                stepForwardTspBttn.IsEnabled = false;

                // задание изображения по умолчанию
                try
                {
                    tspImg.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Painter.Drawing(graph).GetHbitmap(),
                        IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
        }

        // предыдущий шаг метода ветвей и границ
        private void StepBackTsp(object sender, RoutedEventArgs e)
        {
            if (iterator.MovePrevious())
            {
                try
                {
                    tspImg.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(iterator.Current.GetHbitmap(),
                        IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // возможность перейти на следующий шаг метода ветвей и границ
                stepForwardTspBttn.IsEnabled = true;
            }
            else
                // невозможность перейи на предыдущий шаг метода ветвей и границ
                stepBackTspBttn.IsEnabled = false;
        }

        // следущий шаг метода ветвей и границ
        private void StepForwardTsp(object sender, RoutedEventArgs e)
        {
            if (iterator.MoveNext())
            {
                try
                {
                    tspImg.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(iterator.Current.GetHbitmap(),
                        IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // возможность перейти на предыдущий шаг метода ветвей и границ
                stepBackTspBttn.IsEnabled = true;
            }
            else
                // невозможность перейи на следущий шаг метода ветвей и границ
                stepForwardTspBttn.IsEnabled = false;
        }

        #endregion
    }
}
