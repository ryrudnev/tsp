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
            Graph g = Graph.CompleteGraphFromFile("input.txt");
            g.Drawing();
            Graph.Path p = new Graph.Path(g);
            p.Append(new Graph.Edge(g, 0, 3, 4));
            p.Append(new Graph.Edge(g, 3, 2, 2));
            p.Append(new Graph.Edge(g, 1, 0, 6));
            p.Append(new Graph.Edge(g, 4, 1, 4));
            p.Append(new Graph.Edge(g, 2, 4, 2));
            bool isOk = p.IsExists();
            isOk = g.IsOwnedPath(p);
            g.Drawing(p);
        }
    }
}
