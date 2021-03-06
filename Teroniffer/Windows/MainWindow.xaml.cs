﻿using Detrav.TeraApi;
using Detrav.TeraApi.OpCodes;
using Detrav.Teroniffer.Core;
using Microsoft.Win32;
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

namespace Detrav.Teroniffer.Windows
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ((buttonNew as Button).Content as Image).Source = ToImage("Detrav.Teroniffer.assets.images.new.png");
            ((buttonOpen as Button).Content as Image).Source = ToImage("Detrav.Teroniffer.assets.images.open.png");
            ((buttonSave as Button).Content as Image).Source = ToImage("Detrav.Teroniffer.assets.images.save.png");
            ((buttonCopy as Button).Content as Image).Source = ToImage("Detrav.Teroniffer.assets.images.copy.png");
            ((buttonEdit as Button).Content as Image).Source = ToImage("Detrav.Teroniffer.assets.images.edit.png");
            ((buttonCalc as Button).Content as Image).Source = ToImage("Detrav.Teroniffer.assets.images.calculator.png");
            ((buttonWhite as Button).Content as Image).Source = ToImage("Detrav.Teroniffer.assets.images.white.png");
            ((buttonBlack as Button).Content as Image).Source = ToImage("Detrav.Teroniffer.assets.images.black.png");
            ((buttonSearch as Button).Content as Image).Source = ToImage("Detrav.Teroniffer.assets.images.search.png");
            ((buttonBug as Button).Content as Image).Source = ToImage("Detrav.Teroniffer.assets.images.bug.png");
        }

        List<DataPacket> packets = new List<DataPacket>();

        public void doEvents()
        {
            int count = 0;
            lock(packets)
            {
                count = packets.Count;
            }
            labelPacketCount.Content = count.ToString();
            double m = GC.GetTotalMemory(true);
            int order = 0;
            while (m >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                m = m / 1024;
            }

            labelMemoryUsage.Content = String.Format("{0:0.##} {1}", m, sizes[order]);

            if(checkBoxForTimer.IsChecked == true)
            {
                buttonRefresh_Click(this, new RoutedEventArgs());
            }
        }

        private void buttonNew_Click(object sender, RoutedEventArgs e)
        {
            lock (packets)
            {
                packets.Clear();
            }
            buttonRefresh_Click(sender, e);
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Json file (*.json)|*.json";
            sfd.DefaultExt = ".json";
            sfd.InitialDirectory = PacketStructureManager.assets.getMyFolder();
            if (sfd.ShowDialog() == true)
            {
                List<SavedPacketData> list = new List<SavedPacketData>();
                lock (packets)
                {
                    foreach (var p in packets)
                        list.Add(new SavedPacketData(p.type, p.getTeraPacket().data, p.time));
                }
                PacketStructureManager.assets.serialize(sfd.FileName, list.ToArray(), TeraApi.Interfaces.AssetType.global);
            }
        }

        private void buttonEdit_Click(object sender, RoutedEventArgs e)
        {
            StructureWindow w;
            if (dataGrid.SelectedItem != null)
                w = new StructureWindow((dataGrid.SelectedItem as DataPacket).opCode);
            else w = new StructureWindow();
            w.ShowDialog();
        }

        private void buttonWhite_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem != null)
            {
                listBoxWhite.Items.Add((dataGrid.SelectedItem as DataPacket).opCode);
                tabControl.SelectedIndex = 1;
            }
        }

        private void buttonBlack_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem != null)
            {
                listBoxBlack.Items.Add((dataGrid.SelectedItem as DataPacket).opCode);
                tabControl.SelectedIndex = 1;
            }
        }

        private void buttonSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dataGrid.SelectedItem != null)
                {

                    byte[] bb = stringToByteArray(searchBox.Text);


                    MessageBox.Show(byteArrayContaints((dataGrid.SelectedItem as DataPacket).getTeraPacket().data, bb).ToString("X2"));
                }
            }
            catch { MessageBox.Show("ERROR"); }
        }
        private static byte[] stringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        private static int byteArrayContaints(byte[] bb, byte[] what)
        {
            int len = bb.Length - what.Length;
            for (int i = 0; i < len; i++)
            {
                bool flag = true;
                for (int j = 0; j < what.Length; j++)
                {
                    if (bb[i + j] != what[j])
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                    return i;
            }
            return -1;
        }

        private void buttonPrev_Click(object sender, RoutedEventArgs e)
        {

            int take = 1000;
            Int32.TryParse(textBoxCount.Text, out take);
            int skip = 0;
            Int32.TryParse(textBoxSkip.Text, out skip);
            textBoxSkip.Text = (skip - take).ToString();
            buttonRefresh_Click(this, e);
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            int skip = 0;
            Int32.TryParse(textBoxSkip.Text, out skip);
            int take = 1000;
            Int32.TryParse(textBoxCount.Text, out take);
            lock (packets)
            {
                var packs = packets.Select(p => p);
                //Проверка на тип пакета
                if (comboBoxType.SelectedIndex == 1)
                    packs = packs.Where(p => PacketType.Recv == p.type);
                else if (comboBoxType.SelectedIndex == 2)
                    packs = packs.Where(p => PacketType.Send == p.type);
                //Проверка на белый лист
                List<object> ws = new List<object>();
                if (listBoxWhite.Items.Count > 0)
                {

                    foreach (var item in listBoxWhite.Items)
                        ws.Add(item);
                    packs = from p in packs where ws.Contains(p.opCode) select p;
                }
                //Проверка на черный лист
                List<object> bs = new List<object>();
                if (listBoxBlack.Items.Count > 0)
                {

                    foreach (var item in listBoxBlack.Items)
                        bs.Add(item);
                    packs = from p in packs where !bs.Contains(p.opCode) select p;
                }
                //Медлено по строке
                try
                {
                    byte[] bb = stringToByteArray(searchBox.Text);
                    packs = from p in packs where byteArrayContaints(p.getTeraPacket().data, bb) >= 0 select p;
                }
                catch { }
                try
                {
                    string filter = textBoxStringFilter.Text;
                    packs = from p in packs where p.opCode.ToString().IndexOf(filter) >= 0 select p;
                }
                catch { }
                //Поиск будет отдельно
                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = packs.Skip(skip).Take(take);
                dataGrid.Items.Refresh();
            }

        }

        private void buttonNext_Click(object sender, RoutedEventArgs e)
        {
            int take = 1000;
            Int32.TryParse(textBoxCount.Text, out take);
            int skip = 0;
            Int32.TryParse(textBoxSkip.Text, out skip);
            textBoxSkip.Text = (skip + take).ToString();
            buttonRefresh_Click(this, e);
        }

        private void buttonFilterImport_Click(object sender, RoutedEventArgs e)
        {
            FilterStructure f;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Json file (*.json)|*.json";
            ofd.InitialDirectory = PacketStructureManager.assets.getMyFolder();
            ofd.DefaultExt = ".json";
            if (ofd.ShowDialog() != true) return;
            string file = ofd.FileName;
            object obj = PacketStructureManager.assets.deSerialize(file, typeof(FilterStructure), TeraApi.Interfaces.AssetType.global);
            if (obj == null) return;
            f = obj as FilterStructure;
            comboBoxType.SelectedIndex = f.indexRecvSend;
            listBoxWhite.Items.Clear();
            if (f.whiteList != null)
                foreach (var el in f.whiteList)
                    listBoxWhite.Items.Add(PacketCreator.getOpCode(Convert.ToUInt16(el)));
            listBoxBlack.Items.Clear();
            if (f.blackList != null)
                foreach (var el in f.blackList)
                    listBoxBlack.Items.Add(PacketCreator.getOpCode(Convert.ToUInt16(el)));
            textBoxStringFilter.Text = f.filter;
        }

        private void buttonFilterExport_Click(object sender, RoutedEventArgs e)
        {
            FilterStructure f = new FilterStructure()
            {
                indexRecvSend = comboBoxType.SelectedIndex,
                whiteList = new object[listBoxWhite.Items.Count],
                blackList = new object[listBoxBlack.Items.Count]
            };
            for (int i = 0; i < f.whiteList.Length; i++)
                f.whiteList[i] = listBoxWhite.Items[i];
            for (int i = 0; i < f.blackList.Length; i++)
                f.blackList[i] = listBoxBlack.Items[i];
            f.filter = textBoxStringFilter.Text;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".json";
            sfd.Filter = "Json file (*.json)|*.json";
            //sfd.RestoreDirectory = true;
            sfd.InitialDirectory = PacketStructureManager.assets.getMyFolder();
            if(sfd.ShowDialog() == true)
                PacketStructureManager.assets.serialize(sfd.FileName, f, TeraApi.Interfaces.AssetType.global);
            /*if (sfd.ShowDialog() == true)
            {
                using (StreamWriter w = new StreamWriter(sfd.OpenFile()))
                {
                    XmlSerializer xsr = new XmlSerializer(f.GetType());
                    xsr.Serialize(w, f);
                }
            }*/

        }

        static string[] sizes = { "B", "KB", "MB", "GB" };

        public void parsePacket(Detrav.TeraApi.TeraPacketWithData teraPacket)
        {
            lock (packets)
            {
                packets.Add(new DataPacket(packets.Count, teraPacket));
            }
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGrid.SelectedItem != null)
            {
                DataPacket p = (dataGrid.SelectedItem as DataPacket);
                PacketStructure ps = PacketStructureManager.getStructure(p.opCode);
                textBlockPacket.Text = ps.parse(p.getTeraPacket()).ToString();
            }
                //textBlockPacket.Text = Detrav.Sniffer.Tera.TeraPacketCreator.create((dataGrid.SelectedItem as DataPacket).getTeraPacket()).ToString();
            /*richTextBox.Document.Blocks.Clear();
            richTextBox.Selection.Text = Detrav.Sniffer.Tera.TeraPacketCreator.create((dataGrid.SelectedItem as DataPacket).getTeraPacket()).ToString();*/
        }

        private void MenuItem_AddWhite_Click(object sender, RoutedEventArgs e)
        {
            AddPacketWindow w = new AddPacketWindow();
            if (w.ShowDialog() == true)
            {
                listBoxWhite.Items.Add(w.valueEnum);
            }
        }

        private void MenuItem_AddBlack_Click(object sender, RoutedEventArgs e)
        {
            AddPacketWindow w = new AddPacketWindow();
            if (w.ShowDialog() == true)
            {
                listBoxBlack.Items.Add(w.valueEnum);
            }
        }

        private void MenuItem_RemoveWhite_Click(object sender, RoutedEventArgs e)
        {
            while (listBoxWhite.SelectedIndex >= 0)
            {
                listBoxWhite.Items.RemoveAt(listBoxWhite.SelectedIndex);
            }
        }

        private void MenuItem_RemoveBlack_Click(object sender, RoutedEventArgs e)
        {
            while (listBoxBlack.SelectedIndex >= 0)
            {
                listBoxBlack.Items.RemoveAt(listBoxBlack.SelectedIndex);
            }
        }

        private void buttonCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(textBlockPacket.Text);
        }
        public bool close = false;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (close) return;
            Hide();
            e.Cancel = true;
        }

        public BitmapImage ToImage(string filename)
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            using (System.IO.Stream resFilestream = a.GetManifestResourceStream(filename))
            {
                if (resFilestream == null) return null;
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad; // here
                image.StreamSource = resFilestream;
                image.EndInit();
                return image;

            }
        }

        private void buttonCalc_Click(object sender, RoutedEventArgs e)
        {
            //if (dataGrid.SelectedItem != null)
            //{
            CalculatorWindow w = new CalculatorWindow();
            //w.setData((dataGrid.SelectedItem as DataPacket).getTeraPacket().data);
            w.Show();
            //}
        }


        private void buttonBug_Click(object sender, RoutedEventArgs e)
        {

        }
        private void buttonOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Json file (*.json)|*.json";
            ofd.DefaultExt = ".json";
            ofd.InitialDirectory = PacketStructureManager.assets.getMyFolder();
            if (ofd.ShowDialog() == true)
            {
                SavedPacketData[] list = PacketStructureManager.assets.deSerialize(ofd.FileName, typeof(SavedPacketData[]), TeraApi.Interfaces.AssetType.global) as SavedPacketData[];
                lock (packets)
                {
                    packets.Clear();
                    foreach(var el in list)
                    packets.Add(new DataPacket(packets.Count,new TeraPacketWithData(el.data,el.type,el.time)));   
                }
            }
        }
    }
}
