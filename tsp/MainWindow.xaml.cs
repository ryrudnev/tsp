using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace tsp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            
            InitializeComponent();
            
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Graph g = Graph.TakenFromFile("input.txt");
            Graph.Painter.Drawing(g);
            Graph.Path p = new Graph.Path();
            p.Append(new Graph.Edge(0, 3, 4));
            p.Append(new Graph.Edge(3, 2, 2));
            p.Append(new Graph.Edge(1, 0, 6));
            p.Append(new Graph.Edge(4, 1, 4));
            p.Append(new Graph.Edge(2, 4, 2));
            bool isOk = p.IsExists();
            Graph.Painter.Drawing(g, p);
        }
    }
}
