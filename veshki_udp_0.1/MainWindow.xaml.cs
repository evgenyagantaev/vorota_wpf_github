using System;
using System.Collections.Generic;
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

using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace veshki_udp_0._1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void NoArgDelegate();

        SerialPort stm_virtual_com_port;
        Boolean connected_ok = false;

        Boolean manual_timer_started = false;
        int manual_minutes = 0;
        int manual_seconds = 0;
        int manual_santis = 0;
        int manual_seconds_total = 111;
        Boolean auto_timer_started = false;
        int auto_minutes = 0;
        int auto_seconds = 0;
        int auto_santis = 0;
        int auto_seconds_total = 111;

        String file_prefix;
        StreamWriter data_out, auto_data_out;

        public MainWindow()
        {
            InitializeComponent();

            #region создаем строку-префикс имени файла для сохранения данных
            file_prefix = DateTime.Now.ToString("yyyy_MM_dd__HH_mm_ss__");
            #endregion
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dataReceivingThread = new Thread(new ThreadStart(this.dataReceivingMethod));
            dataReceivingThread.IsBackground = true;

            

        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////
        private Thread dataReceivingThread;

        private void dataReceivingMethod()
        {
            

            while(true)
            {
                string com_input_string = stm_virtual_com_port.ReadLine();

                if (com_input_string.Contains("START"))
                {
                    //start_timer();
                    Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                            new NoArgDelegate(auto_start_timer));
                }
                else if (com_input_string.Contains("STOP"))
                {
                    //stop_timer();
                    Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                            new NoArgDelegate(auto_stop_timer));
                }
            }
        }

        private Thread sleepingThread;

        private void sleepingMethod()
        {
            Thread.Sleep(3000);
        }

        private Thread manual_timerThread;

        private void manual_timerMethod()
        {
            long start_ticks = DateTime.Now.Ticks;
            long time_from_start = 0;
            while (manual_timer_started)
            {
                Thread.Sleep(10);
                time_from_start = DateTime.Now.Ticks - start_ticks;

                int santis_total = (int)(time_from_start / 100000L);
                // kalibrovka
                //santis_total += ((santis_total / 100) / 5) * 2 + 2;
                //seconds_total = (int)(time_from_start / 10000000L);
                manual_seconds_total = santis_total / 100;
                manual_minutes = manual_seconds_total / 60;
                manual_seconds = manual_seconds_total - manual_minutes * 60;
                manual_santis = santis_total - manual_seconds_total * 100;

                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                            new NoArgDelegate(manual_UpdateTimer));
            }

        }

        private Thread auto_timerThread;

        private void auto_timerMethod()
        {
            long start_ticks = DateTime.Now.Ticks;
            long time_from_start = 0;
            while (auto_timer_started)
            {
                Thread.Sleep(10);
                time_from_start = DateTime.Now.Ticks - start_ticks;

                int santis_total = (int)(time_from_start / 100000L);
                // kalibrovka
                //santis_total += ((santis_total / 100) / 5) * 2 + 2;
                //seconds_total = (int)(time_from_start / 10000000L);
                auto_seconds_total = santis_total / 100;
                auto_minutes = auto_seconds_total / 60;
                auto_seconds = auto_seconds_total - auto_minutes * 60;
                auto_santis = santis_total - auto_seconds_total * 100;

                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                            new NoArgDelegate(auto_UpdateTimer));
            }

        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void UpdateUserInterface()
        {
           
        }


        private void manual_start_timer()
        {
            if (!manual_timer_started)
            {
                manual_timer_started = true;

                manual_timerThread = new Thread(new ThreadStart(this.manual_timerMethod));
                manual_timerThread.IsBackground = true;
                manual_timerThread.Start();

                try
                {
                    data_out = new StreamWriter(@"c:\temp\" + file_prefix + "timing.txt", true);
                    #region создаем таймстамп для сохранения данных
                    String timestamp = DateTime.Now.ToString("   HH:mm:ss.ff   yyyy.MM.dd");
                    #endregion
                    data_out.WriteLine("start -> " + timestamp);
                    data_out.Close();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                    Environment.Exit(1);
                } 
            }
        }

        private void auto_start_timer()
        {
            if (!auto_timer_started)
            {
                auto_timer_started = true;

                auto_timerThread = new Thread(new ThreadStart(this.auto_timerMethod));
                auto_timerThread.IsBackground = true;
                auto_timerThread.Start();

                try
                {
                    auto_data_out = new StreamWriter(@"c:\temp\" + file_prefix + "auto_timing.txt", true);
                    #region создаем таймстамп для сохранения данных
                    String timestamp = DateTime.Now.ToString("   HH:mm:ss.ff   yyyy.MM.dd");
                    #endregion
                    auto_data_out.WriteLine("auto start -> " + timestamp);
                    auto_data_out.Close();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                    Environment.Exit(1);
                }
            }
        }

        private void manual_stop_timer()
        {
            if (manual_timer_started)
            {
                manual_timer_started = false;
                manual_seconds_total = 0;

                try
                {
                    data_out = new StreamWriter(@"c:\temp\" + file_prefix + "timing.txt", true);
                    #region создаем таймстамп для сохранения данных
                    String timestamp = DateTime.Now.ToString("   HH:mm:ss.ff   yyyy.MM.dd");
                    #endregion
                    string time_elapsed = manual_minutes.ToString("D2") + ":" + manual_seconds.ToString("D2") + "." + manual_santis.ToString("D2");
                    data_out.WriteLine("stop -> " + time_elapsed + " -- " + name_textbox.Text + timestamp);
                    data_out.Close();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                    Environment.Exit(1);
                } 
            }
        }

        private void auto_stop_timer()
        {
            if (auto_timer_started)
            {
                auto_timer_started = false;
                auto_seconds_total = 0;

                try
                {
                    auto_data_out = new StreamWriter(@"c:\temp\" + file_prefix + "auto_timing.txt", true);
                    #region создаем таймстамп для сохранения данных
                    String timestamp = DateTime.Now.ToString("   HH:mm:ss.ff   yyyy.MM.dd");
                    #endregion
                    string time_elapsed = auto_minutes.ToString("D2") + ":" + auto_seconds.ToString("D2") + "." + auto_santis.ToString("D2");
                    auto_data_out.WriteLine("auto stop -> " + time_elapsed + " -- " + name_textbox.Text + timestamp);
                    auto_data_out.Close();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                    Environment.Exit(1);
                }
            }
        }

        private void manual_UpdateTimer()
        {
            string time_elapsed = manual_minutes.ToString("D2") + ":" + manual_seconds.ToString("D2") + "." + manual_santis.ToString("D2");
            timer_label.Content = time_elapsed;
        }

        private void auto_UpdateTimer()
        {
            string auto_time_elapsed = auto_minutes.ToString("D2") + ":" + auto_seconds.ToString("D2") + "." + auto_santis.ToString("D2");
            auto_timer_label.Content = auto_time_elapsed;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void start_stop_button_Click(object sender, RoutedEventArgs e)
        {
            //*
            if (!manual_timer_started)
            {
                manual_start_timer();
                start_stop_button.Content = "Stop";
                
            }
            else if (manual_timer_started)
            {
                manual_stop_timer();
                start_stop_button.Content = "Start";
                
            }
            //*/

            /*
            if (!auto_timer_started)
            {
                auto_start_timer();
                start_stop_button.Content = "Stop";

            }
            else if (auto_timer_started)
            {
                auto_stop_timer();
                start_stop_button.Content = "Start";

            }
            */
        }

        private void reset_button_Click(object sender, RoutedEventArgs e)
        {
            if (!manual_timer_started)
            {
                manual_minutes = 0;
                manual_seconds = 0;
                manual_santis = 0;
                manual_UpdateTimer();
            }

            if (!auto_timer_started)
            {
                auto_minutes = 0;
                auto_seconds = 0;
                auto_santis = 0;
                auto_UpdateTimer();
            }
        }

        private void auto_stop_button_Click(object sender, RoutedEventArgs e)
        {
            if (auto_timer_started)
            {
                auto_stop_timer();

            }
        }

        private void connect_button_Click(object sender, RoutedEventArgs e)
        {
            stm_virtual_com_port = new SerialPort("COM" + portnumber_textbox.Text, 115200, Parity.None, 8, StopBits.One);

            try
            {
                stm_virtual_com_port.Open();
                if (stm_virtual_com_port.IsOpen)
                {
                    connected_ok = true;
                    connection_label.Content = "Sensor OK";
                }
                else
                    connection_label.Content = "No sensor";

            }
            catch (Exception ee)
            {
                string exception_string = ee.ToString();
                connection_label.Content = "No sensor";
            }

            portnumber_textbox.Visibility = System.Windows.Visibility.Hidden;
            connect_button.Visibility = System.Windows.Visibility.Hidden;

            if (connected_ok)
                dataReceivingThread.Start();
        }
    }
}
