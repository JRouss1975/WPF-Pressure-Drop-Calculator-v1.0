using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using MathNet.Numerics.Optimization.LineSearch;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Text.RegularExpressions;

namespace WPF_PressureDrop
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<HeadLossCalc> lstHeadLossCalcs = new ObservableCollection<HeadLossCalc>();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = lstHeadLossCalcs;
           
        }

        private void menuNew_Click(object sender, RoutedEventArgs e)
        {
            lstHeadLossCalcs.Clear();
            txtPCF.Text = "";
        }

        private void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog _openFileDialog = new OpenFileDialog();
            if (_openFileDialog.ShowDialog() == true)
            {
                try
                {
                    XmlSerializer _xmlFormatter = new XmlSerializer(typeof(ObservableCollection<HeadLossCalc>));
                    using (Stream _fileStream = new FileStream(_openFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        _fileStream.Position = 0;
                        lstHeadLossCalcs = (ObservableCollection<HeadLossCalc>)_xmlFormatter.Deserialize(_fileStream);
                    }
                    dgvCalc.ItemsSource = lstHeadLossCalcs;

                    Draw();
                }
                catch (Exception)
                {
                    MessageBox.Show("Please try open the correct file type.");
                }
            }
        }

        private void menuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog _saveFileDialog = new SaveFileDialog();
            if (_saveFileDialog.ShowDialog() == true)
            {
                XmlSerializer _xmlFormatter = new XmlSerializer(typeof(ObservableCollection<HeadLossCalc>));
                using (Stream _fileStream = new FileStream(_saveFileDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    _fileStream.Position = 0;
                    _xmlFormatter.Serialize(_fileStream, lstHeadLossCalcs);
                }
            }
        }

        private void menuImport_Click(object sender, RoutedEventArgs e)
        {
            //Local Variables
            List<string> lstLines = new List<string>();
            List<string> selectedLines = new List<string>();
            string[] Headers = new string[] { "WELD", "PIPE", "FLANGE", "GASKET", "END-CONNECTION-PIPELINE", "VALVE", "INDUCTION-START", "BEND", "INDUCTION-END", "MESSAGE-ROUND", "REDUCER-CONCENTRIC" };

            Double.TryParse(txtTemp.Text, out double t);
            Double.TryParse(txte.Text, out double ep);
            Double.TryParse(txtQ.Text, out double q);
            double temperature = t;
            double epsilon = ep;
            double qh = q;

            //Open File
            OpenFileDialog _openFileDialog = new OpenFileDialog();
            if (_openFileDialog.ShowDialog() == true)
            {
                using (Stream _fileStream = new FileStream(_openFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    string txt = "";
                    using (var streamReader = new StreamReader(_fileStream, Encoding.UTF8, true, 128))
                    {
                        String line;
                        int n = 0;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            lstLines.Add(line);
                            n = ++n;
                            txt = txt + (n + "... " + line + "\n");
                        }
                        txtPCF.Text = txt;
                    }
                }
            }

            //Search for Items
            for (int i = 0; i < lstLines.Count - 1; i++)
            {

                if (lstLines[i] == "PIPE")
                {
                    selectedLines.Add(lstLines[i]);

                    HeadLossCalc pd = new HeadLossCalc(i + 1);
                    pd.ElementType = ItemType.Pipe;
                    pd.t = temperature;
                    pd.epsilon = epsilon;
                    pd.qh = qh;

                    int c = 1;
                    bool endConditionFound = false;

                    string str = "";
                    List<string> p1 = new List<string>();
                    List<string> p2 = new List<string>();
                    List<string> w0 = new List<string>();

                    do
                    {
                        if ((i + c) > lstLines.Count - 1) break;
                        str = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList().First();

                        if (str == "END-POINT")
                        {
                            selectedLines.Add(lstLines[i + c]);
                            p1 = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            selectedLines.Add(lstLines[i + c + 1]);
                            p2 = lstLines[i + c + 1].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            i = i + 2;
                            pd.Node1.X = Double.Parse(p1[1]);
                            pd.Node1.Y = Double.Parse(p1[2]);
                            pd.Node1.Z = Double.Parse(p1[3]);
                            pd.Node2.X = Double.Parse(p2[1]);
                            pd.Node2.Y = Double.Parse(p2[2]);
                            pd.Node2.Z = Double.Parse(p2[3]);
                            pd.d = Double.Parse(p1[4]);
                            pd.L = pd.Distance(pd.Node1.X, pd.Node2.X, pd.Node1.Y, pd.Node2.Y, pd.Node1.Z, pd.Node2.Z) / 1000;
                        }

                        if (str == "WEIGHT")
                        {
                            w0 = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            pd.W = Double.Parse(w0[1]) * pd.L;
                        }

                        if (Headers.Contains(str))
                        {
                            endConditionFound = true;
                        }

                        c++;
                    } while (!endConditionFound);

                    lstHeadLossCalcs.Add(pd);

                }

                if (lstLines[i] == "REDUCER-CONCENTRIC")
                {
                    selectedLines.Add(lstLines[i]);

                    HeadLossCalc pd = new HeadLossCalc(i + 1);
                    pd.t = temperature;
                    pd.epsilon = epsilon;
                    pd.qh = qh;

                    int c = 1;
                    bool endConditionFound = false;

                    string str = "";
                    double d1 = 0;
                    double d2 = 0;
                    List<string> p1 = new List<string>();
                    List<string> p2 = new List<string>();
                    List<string> w0 = new List<string>();

                    do
                    {
                        if ((i + c) > lstLines.Count - 1) break;
                        str = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList().First();

                        if (str == "END-POINT")
                        {
                            selectedLines.Add(lstLines[i + c]);
                            p1 = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            selectedLines.Add(lstLines[i + c + 1]);
                            p2 = lstLines[i + c + 1].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            i = i + 2;
                            pd.Node1.X = Double.Parse(p1[1]);
                            pd.Node1.Y = Double.Parse(p1[2]);
                            pd.Node1.Z = Double.Parse(p1[3]);
                            pd.Node2.X = Double.Parse(p2[1]);
                            pd.Node2.Y = Double.Parse(p2[2]);
                            pd.Node2.Z = Double.Parse(p2[3]);
                            pd.d = Double.Parse(p1[4]);
                            d1 = Double.Parse(p1[4]);
                            d2 = Double.Parse(p2[4]);
                            pd.L = pd.Distance(pd.Node1.X, pd.Node2.X, pd.Node1.Y, pd.Node2.Y, pd.Node1.Z, pd.Node2.Z) / 1000;

                            if (d1 < d2)
                            {
                                pd.ElementType = ItemType.Expander;
                                pd.d = Double.Parse(p1[4]);
                                pd.d1 = d1;
                                pd.d2 = d2;
                            }
                            else
                            {
                                pd.ElementType = ItemType.Reducer;
                                pd.d = Double.Parse(p1[4]);
                                pd.d1 = d2;
                                pd.d2 = d1;
                            }
                        }

                        if (str == "WEIGHT")
                        {
                            w0 = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            pd.W = Double.Parse(w0[1]) * pd.L;
                        }


                        if (Headers.Contains(str))
                        {
                            endConditionFound = true;
                        }

                        c++;
                    } while (!endConditionFound);

                    lstHeadLossCalcs.Add(pd);
                }

                if (lstLines[i] == "BEND")
                {
                    selectedLines.Add(lstLines[i]);

                    HeadLossCalc pd = new HeadLossCalc(i + 1);
                    pd.ElementType = ItemType.Bend;
                    pd.t = temperature;
                    pd.epsilon = epsilon;
                    pd.qh = qh;

                    int c = 1;
                    bool endConditionFound = false;

                    string str = "";
                    List<string> p1 = new List<string>();
                    List<string> p2 = new List<string>();
                    List<string> c1 = new List<string>();
                    List<string> a0 = new List<string>();
                    List<string> r0 = new List<string>();
                    List<string> w0 = new List<string>();
                    double a = 0;
                    double r = 0;

                    do
                    {
                        if ((i + c) > lstLines.Count - 1) break;
                        str = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList().First();

                        if (str == "END-POINT")
                        {
                            selectedLines.Add(lstLines[i + c]);
                            p1 = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            selectedLines.Add(lstLines[i + c + 1]);
                            p2 = lstLines[i + c + 1].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            i = i + 2;
                            pd.Node1.X = Double.Parse(p1[1]);
                            pd.Node1.Y = Double.Parse(p1[2]);
                            pd.Node1.Z = Double.Parse(p1[3]);
                            pd.Node2.X = Double.Parse(p2[1]);
                            pd.Node2.Y = Double.Parse(p2[2]);
                            pd.Node2.Z = Double.Parse(p2[3]);
                            pd.d = Double.Parse(p1[4]);
                        }

                        if (str == "ANGLE")
                        {
                            selectedLines.Add(lstLines[i + c]);
                            a0 = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            a = Double.Parse(a0[1]);
                            pd.n = (a / 100) / 90;
                        }

                        if (str == "BEND-RADIUS")
                        {
                            selectedLines.Add(lstLines[i + c]);
                            r0 = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            r = Double.Parse(r0[1]);
                            pd.r = r;
                            pd.L = (r / 1000) * (Math.PI * (a / 100) / 180);
                        }

                        if (str == "WEIGHT")
                        {
                            w0 = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            pd.W = Double.Parse(w0[1]) * pd.L;
                        }

                        if (Headers.Contains(str))
                        {
                            endConditionFound = true;
                        }

                        c++;
                    } while (!endConditionFound);

                    lstHeadLossCalcs.Add(pd);
                }

                if (lstLines[i] == "VALVE")
                {
                    selectedLines.Add(lstLines[i]);

                    HeadLossCalc pd = new HeadLossCalc(i + 1);
                    pd.ElementType = ItemType.Butterfly;
                    pd.t = temperature;
                    pd.epsilon = epsilon;
                    pd.qh = qh;

                    int c = 1;
                    bool endConditionFound = false;

                    string str = "";
                    List<string> p1 = new List<string>();
                    List<string> p2 = new List<string>();
                    List<string> w0 = new List<string>();

                    do
                    {
                        if ((i + c) > lstLines.Count - 1) break;
                        str = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList().First();

                        if (str == "END-POINT")
                        {
                            selectedLines.Add(lstLines[i + c]);
                            p1 = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            selectedLines.Add(lstLines[i + c + 1]);
                            p2 = lstLines[i + c + 1].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            i = i + 2;
                            pd.Node1.X = Double.Parse(p1[1]);
                            pd.Node1.Y = Double.Parse(p1[2]);
                            pd.Node1.Z = Double.Parse(p1[3]);
                            pd.Node2.X = Double.Parse(p2[1]);
                            pd.Node2.Y = Double.Parse(p2[2]);
                            pd.Node2.Z = Double.Parse(p2[3]);
                            pd.d = Double.Parse(p1[4]);
                            pd.L = pd.Distance(pd.Node1.X, pd.Node2.X, pd.Node1.Y, pd.Node2.Y, pd.Node1.Z, pd.Node2.Z) / 1000;
                        }

                        if (str == "WEIGHT")
                        {
                            w0 = lstLines[i + c].Split(' ').ToList<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            pd.W = Double.Parse(w0[1]);
                        }

                        if (Headers.Contains(str))
                        {
                            endConditionFound = true;
                        }

                        c++;
                    } while (!endConditionFound);

                    lstHeadLossCalcs.Add(pd);
                }
            }

            //Draw Model
            Draw();
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void menuRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (dgvCalc.Items.Count > 0 && dgvCalc.SelectedIndex > -1)
            {
                var selection = dgvCalc.SelectedItems;

                List<HeadLossCalc> lstSelection = new List<HeadLossCalc>();
                foreach (var item in selection)
                {
                    lstSelection.Add((HeadLossCalc)item);
                }

                foreach (HeadLossCalc item in lstSelection)
                {
                    lstHeadLossCalcs.Remove(item);
                }
            }
            else
            {
                MessageBox.Show("No item to remove!");
            }
        }

        private void menuAddItem_Click(object sender, RoutedEventArgs e)
        {
            Double.TryParse(txtTemp.Text, out double t);
            Double.TryParse(txte.Text, out double ep);
            Double.TryParse(txtQ.Text, out double q);

            int maxId = 0;

            if (lstHeadLossCalcs.Count > 0)
                maxId = lstHeadLossCalcs.Max(x => x.ElementId);


            if (dgvCalc.SelectedIndex == lstHeadLossCalcs.Count - 1 || dgvCalc.SelectedIndex == -1)
            {
                lstHeadLossCalcs.Add(new HeadLossCalc() { ElementId = maxId + 1, t = t, epsilon = ep, qh = q });
                return;
            }


            if (dgvCalc.SelectedIndex > -1)
            {
                lstHeadLossCalcs.Insert(dgvCalc.SelectedIndex, new HeadLossCalc() { ElementId = maxId + 1, t = t, epsilon = ep, qh = q });
            }


        }

        private void DgvCalc_SelectionChanged(object sender, Xceed.Wpf.DataGrid.DataGridSelectionChangedEventArgs e)
        {
            HeadLossCalc selectedItem = (HeadLossCalc)e.SelectionInfos[0].DataGridContext.CurrentItem;
            if (selectedItem != null)
            {
                double totalHl = 0;
                double totalV = 0;
                double totalL = 0;
                var selection = dgvCalc.SelectedItems;
                foreach (HeadLossCalc hlc in selection)
                {
                    totalL = totalL + hlc.L;
                    totalV = totalV + hlc.Vm;
                    totalHl = totalHl + hlc.hLm;

                }
                tbL.Text = totalL.ToString("N3");
                tbV.Text = totalV.ToString("N3");
                tbHlmm.Text = totalHl.ToString("N2");
            }
        }

        private void btnApplyAll_Click(object sender, RoutedEventArgs e)
        {
            Double.TryParse(txtTemp.Text, out double t);
            Double.TryParse(txte.Text, out double ep);
            Double.TryParse(txtQ.Text, out double q);
            Double.TryParse(txtL.Text, out double l);
            Double.TryParse(txtD.Text, out double d);
            foreach (HeadLossCalc hlc in lstHeadLossCalcs)
            {
                if (cbTemp.IsChecked == true) hlc.t = t;
                if (cbe.IsChecked == true) hlc.epsilon = ep;
                if (cbQ.IsChecked == true) hlc.qh = q;
                if (cbL.IsChecked == true) hlc.L = l;
                if (cbD.IsChecked == true) hlc.d = d;
            }
        }

        private void btnApplySelection_Click(object sender, RoutedEventArgs e)
        {
            Double.TryParse(txtTemp.Text, out double t);
            Double.TryParse(txte.Text, out double ep);
            Double.TryParse(txtQ.Text, out double q);
            Double.TryParse(txtL.Text, out double l);
            Double.TryParse(txtD.Text, out double d);
            var selection = dgvCalc.SelectedItems;
            foreach (HeadLossCalc hlc in selection)
            {
                if (cbTemp.IsChecked == true) hlc.t = t;
                if (cbe.IsChecked == true) hlc.epsilon = ep;
                if (cbQ.IsChecked == true) hlc.qh = q;
                if (cbL.IsChecked == true) hlc.L = l;
                if (cbD.IsChecked == true) hlc.d = d;
            }
        }

        private void menuSortItem_Click(object sender, RoutedEventArgs e)
        {
            if (lstHeadLossCalcs.Count > 0)
            {
                var items = lstHeadLossCalcs.OrderBy(x => x.ElementId).ToList();
                lstHeadLossCalcs.Clear();

                foreach (var item in items)
                {
                    lstHeadLossCalcs.Add(item);
                }
            }
        }

        private void menuRenumberItem_Click(object sender, RoutedEventArgs e)
        {
            int i = -1;
            Int32.TryParse(tbNumber.Text, out i);
            var selection = dgvCalc.SelectedItems;
            if (selection.Count > 0)
            {
                foreach (HeadLossCalc hlc in selection)
                {
                    hlc.ElementId = i++;
                }
            }

            //Sort list
            var temp = lstHeadLossCalcs.OrderBy(x => x.ElementId).ToList();
            lstHeadLossCalcs.Clear();
            foreach (HeadLossCalc h in temp)
            {
                lstHeadLossCalcs.Add(h);
            }

        }

        private List<HeadLossCalc> copiedItems ;
        private void menuCopyItem_Click(object sender, RoutedEventArgs e)
        {
            copiedItems = new List<HeadLossCalc>();
            var selection = dgvCalc.SelectedItems;
            if (selection.Count > 0)
            {
                foreach (HeadLossCalc hlc in selection)
                {
                    copiedItems.Add(hlc);
                }
            }
        }

        private void menuPasteItem_Click(object sender, RoutedEventArgs e)
        {
            int pos = dgvCalc.SelectedIndex;
            if (copiedItems.Count > 0 && pos > -1)
            {
                foreach (HeadLossCalc hlc in copiedItems)
                {
                    HeadLossCalc newItem = (HeadLossCalc)hlc.Clone();
                    newItem.Node1 = new Node();
                    newItem.Node2 = new Node();
                    lstHeadLossCalcs.Insert(++pos, newItem);
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Draw();
        }

        public void Draw()
        {
            //3D Drawing
            BillboardTextVisual3D txt1;
            LinesVisual3D linesVisual;
            PointsVisual3D pointsVisual;
            Point3DCollection pts = new Point3DCollection();

            View1.Children.Clear();

            foreach (HeadLossCalc i in lstHeadLossCalcs)
            {
                Point3D p1 = new Point3D(i.Node1.X, i.Node1.Y, i.Node1.Z);
                pts.Add(p1);
                Point3D p2 = new Point3D(i.Node2.X, i.Node2.Y, i.Node2.Z);
                pts.Add(p2);
                txt1 = new BillboardTextVisual3D();
                txt1.Text = i.ElementId.ToString();
                txt1.Position = new Point3D((i.Node1.X + i.Node2.X) / 2, (i.Node1.Y + i.Node2.Y) / 2, (i.Node1.Z + i.Node2.Z) / 2);
                View1.Children.Add(txt1);
            }

            GridLinesVisual3D grid = new GridLinesVisual3D();
            grid.Length = 50000;
            grid.Width = 50000;
            grid.MajorDistance = 10000;
            grid.MinorDistance = 1000;
            grid.Visible = true;
            grid.Thickness = 10;
            View1.Children.Add(grid);

            pointsVisual = new PointsVisual3D { Color = Colors.Red, Size = 4 };
            pointsVisual.Points = pts;
            View1.Children.Add(pointsVisual);
            linesVisual = new LinesVisual3D { Color = Colors.Blue };
            linesVisual.Points = pts;
            linesVisual.Thickness = 2;
            View1.Children.Add(linesVisual);
            View1.ZoomExtents(10);

        }

        private void View1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewport = (HelixViewport3D)sender;
            var firstHit = viewport.Viewport.FindHits(e.GetPosition(viewport)).FirstOrDefault();
            if (firstHit != null)
            {
                if (firstHit.Visual is BillboardTextVisual3D)
                {
                    BillboardTextVisual3D t = (BillboardTextVisual3D)firstHit.Visual;
                    dgvCalc.CurrentItem = lstHeadLossCalcs.FirstOrDefault(x => x.ElementId == Convert.ToInt32(t.Text.ToString()));
                }
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void menuExportImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog _saveFileDialog = new SaveFileDialog();
            if (_saveFileDialog.ShowDialog() == true)
            {
                View1.Export(_saveFileDialog.FileName);
            }
        }
    }
}
