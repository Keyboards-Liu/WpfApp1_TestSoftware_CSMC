using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfApp1_TestSoftware_CSMC
{
    /// <summary>
    /// SerialBaseUserControl.xaml 的交互逻辑
    /// </summary>

    public partial class SerialBase : UserControl
    {
        #region 定义内部变量
        private SerialPort serial = new SerialPort();
        private string receiveData;
        private DispatcherTimer autoSendTimer = new DispatcherTimer();
        private DispatcherTimer autoDetectionTimer = new DispatcherTimer();

        public static uint receiveBytesCount = 0;
        public static uint sendBytesCount = 0;

        private Encoding setEncoding = Encoding.Unicode;
        #endregion

        #region 初始化/串口检测
        public SerialBase()// 串口初始化
        {
            InitializeComponent();// 初始化组件
            GetValuablePortName();// 串口检测
            // 设置自动检测50毫秒1次
            autoDetectionTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            autoDetectionTimer.Tick += new EventHandler(AutoDetectionTimer_Tick);
            // 开启定时器
            autoDetectionTimer.Start();
            // 设置状态栏提示
            statusTextBlock.Text = "准备就绪";
        }
        private void GetValuablePortName()// 自动检测串口名
        {
            // 检测有效的串口并添加到ComboBox
            string[] serialPortName = SerialPort.GetPortNames();
            foreach (string name in serialPortName)
            {
                portNameComboBox.Items.Add(name);
            }
        }
        private void AutoDetectionTimer_Tick(object sender, EventArgs e)// 自动检测串口时间
        {
            string[] serialPortName = SerialPort.GetPortNames();
            if (turnOnButton.IsChecked == true)
            {
                // 在有效串口号中遍历当前打开的串口号
                foreach (string name in serialPortName)
                {
                    if (serial.PortName == name)
                        return;// 找到串口，就跳出循环
                }
                // 如果找不到, 说明串口失效了
                // 按钮回弹，从列表中移除串口名
                turnOnButton.IsChecked = false;
                portNameComboBox.Items.Remove(serial.PortName);
                portNameComboBox.SelectedIndex = 0;
                // 提示信息
                statusTextBlock.Text = "串口已失效";
            }
            else
            {
                //检查有效串口和ComboBox中的串口号个数是否不同
                if (portNameComboBox.Items.Count != serialPortName.Length)
                {
                    //串口数不同，清空ComboBox
                    portNameComboBox.Items.Clear();
                    //重新添加有效串口
                    foreach (string name in serialPortName)
                    {
                        portNameComboBox.Items.Add(name);
                    }
                    portNameComboBox.SelectedIndex = 0;
                    statusTextBlock.Text = "串口列表已更新！";
                }
            }

        }
        #endregion

        #region 串口配置面板
        private void SerialSettingControlState(bool state) // 使能串口配置的相关控件
        {
            portNameComboBox.IsEnabled = state;
            baudRateComboBox.IsEnabled = state;
            parityComboBox.IsEnabled = state;
            dataBitsComboBox.IsEnabled = state;
            stopBitsComboBox.IsEnabled = state;
        }
        private void TurnOnButton_Checked(object sender, RoutedEventArgs e)// 打开串口
        {
            try
            {
                //配置串口
                serial.PortName = portNameComboBox.Text;
                serial.BaudRate = Convert.ToInt32(baudRateComboBox.Text);
                serial.Parity = (Parity)Enum.Parse(typeof(Parity), parityComboBox.Text);
                serial.DataBits = Convert.ToInt16(dataBitsComboBox.Text);
                serial.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBitsComboBox.Text);

                //设置串口编码为default：获取操作系统的当前 ANSI 代码页的编码。
                serial.Encoding = setEncoding;

                //添加串口事件处理
                serial.DataReceived += new SerialDataReceivedEventHandler(ReceiveData);

                //开启串口
                serial.Open();

                //关闭串口配置面板
                SerialSettingControlState(false);

                statusTextBlock.Text = "串口已开启";

                //显示提示文字
                turnOnButton.Content = "关闭串口";

                serialPortStatusEllipse.Fill = Brushes.Green;

                //使能发送面板
                // sendControlBorder.IsEnabled = true;


            }
            catch
            {
                statusTextBlock.Text = "配置串口出错！";
            }
        }
        private void TurnOnButton_Unchecked(object sender, RoutedEventArgs e)// 关闭串口
        {
            try
            {
                serial.Close();
                // 关闭定时器
                autoSendTimer.Stop();
                // 使能串口配置面板
                SerialSettingControlState(true);

                statusTextBlock.Text = "串口已关闭";

                // 显示提示文字
                turnOnButton.Content = "打开串口";

                serialPortStatusEllipse.Fill = Brushes.Gray;
                // 使能发送面板
                // sendControlBorder.IsEnabled = false;
            }
            catch
            {

            }
        }
        #endregion

        #region 接收显示窗口
        private delegate void UpdateUiTextDelegate(string text);// 接收数据
        private void ReceiveData(object sender, SerialDataReceivedEventArgs e)
        {
            //Thread.Sleep(50);
            //int len = serial.BytesToRead;
            //byte[] receiveBuffer = new byte[serial.DataBits];
            //if (len != 0)
            //{
            //    serial.Read(receiveBuffer, 0, serial.DataBits);
            //    receiveData = Encoding.Default.GetString(receiveBuffer);
            //}
            this.Dispatcher.Invoke(new Action(delegate
            {
                if (hexadecimalDisplayCheckBox.IsChecked == false)
                {
                    receiveData = serial.ReadExisting();
                    Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(ShowData), receiveData);
                }
                else
                {
                    byte[] receiveBuffer = new byte[serial.BytesToRead];
                    serial.Read(receiveBuffer, 0, receiveBuffer.Length);
                    for (int i = 0; i < receiveBuffer.Length; i++)
                    {
                        Console.Write(receiveBuffer[i] + " ");
                    }
                    // StringtoHexString
                    string result = string.Empty;
                    Console.WriteLine();
                    for (int i = 0; i < receiveBuffer.Length; i++)
                    {
                        result += string.Format("{0:X2} ", receiveBuffer[i]);
                        Console.WriteLine(result);
                    }
                    Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(ShowData), result);
                }
            }));
            
        }

        //private string StringtoHexString(byte[] buffer)
        //{
        //    //string result = string.Empty;
        //    //char[] values = s.ToCharArray();
        //    //foreach (char letter in values)
        //    //{
        //    //    // Get the integral value of the character.
        //    //    int value = Convert.ToInt32(letter);
        //    //    result += string.Format("{0:X2} ", value);
        //    //    // Convert the integer value to a hexadecimal value in string form.
        //    //    Console.WriteLine($"Hexadecimal value of {letter} is {value:X}");
        //    //}
        //    //return result;

        //    //string result = string.Empty;
        //    //byte[] recData = setEncoding.GetBytes(s);

        //    byte[] buffer = reiceiveBuffer;
        //    string result = string.Empty;
        //    foreach (byte str in buffer)
        //    {
        //        int value = Convert.ToInt32(str);
        //        result += string.Format("{0:X2} ", str);
        //        Console.WriteLine($"Hexadecimal value of {str} is {value:X}");
        //        Console.WriteLine(result);
        //    }
        //    return result;

        //    //byte[] recData = Encoding.Default.GetBytes(s);
        //    //return BitConverter.ToString(recData);
        //}
        private void ShowData(string text)
        {
            string receiveText = text;

            // 更新接收字节数
            receiveBytesCount += (uint)receiveText.Length;
            statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();

            // 没有关闭数据显示
            if (stopShowingButton.IsChecked == false && receiveText.Length >= 0)
            {
                // 字符串显示
                receiveTextBox.AppendText(DateTime.Now.ToString() + " <-- ");
                if (hexadecimalDisplayCheckBox.IsChecked == false) // 直接显示
                {
                    receiveTextBox.AppendText(receiveText + " ");
                }
                else // 16进制显示
                {
                    receiveTextBox.AppendText(receiveText);
                }
                receiveTextBox.AppendText("\r\n");
            }
        }

        private void ReceiveTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (receiveTextBox.LineCount >= 50 && autoClearCheckBox.IsChecked == true)
            {
                receiveTextBox.Clear();
            }
            else
            {
                try
                {
                    receiveScrollViewer.ScrollToEnd();
                }
                catch
                {

                }
            }
        }
        #endregion

        #region 接收设置面板
        private void ClearReceiveButton_Click(object sender, RoutedEventArgs e)// 清空接收数据
        {
            receiveTextBox.Clear();
        }
        #endregion

        #region 发送控制面板
        private void SerialPortSend()// 发送数据
        {
            if (!serial.IsOpen)
            {
                statusTextBlock.Text = "请先打开串口！";
                return;
            }
            try
            {
                // 复制发送数据
                string sendData = sendTextBox.Text;

                // 字符串发送
                if (hexadecimalSendCheckBox.IsChecked == false)
                {
                    serial.Write(sendData);

                    // 更新发送数据计数
                    sendBytesCount += (uint)sendData.Length;
                    statusSendByteTextBlock.Text = sendBytesCount.ToString();

                }
                else // 十六进制发送
                {
                    try
                    {
                        sendData.Replace("0x", "");   // 去掉0x
                        sendData.Replace("0X", "");   // 去掉0X
                        //  发送数据
                        string[] strArray = sendData.Split(new char[] { ',', '，', '\r', '\n', ' ', '\t' });
                        int decNum = 0;
                        int i = 0;
                        byte[] sendBuffer = new byte[strArray.Length];  // 发送数据缓冲区

                        foreach (string str in strArray)
                        {
                            try
                            {
                                decNum = Convert.ToInt16(str, 16);
                                sendBuffer[i] = Convert.ToByte(decNum);
                                i++;
                            }
                            catch
                            {
                                MessageBox.Show("字节越界，请逐个字节输入！", "Error");
                            }
                        }

                        serial.Write(sendBuffer, 0, sendBuffer.Length);

                        // 更新发送数据计数
                        sendBytesCount += (uint)sendBuffer.Length;
                        statusSendByteTextBlock.Text = sendBytesCount.ToString();

                    }
                    catch // 无法转为16进制时
                    {
                        autoSendCheckBox.IsChecked = false;// 关闭自动发送
                        statusTextBlock.Text = "当前为16进制发送模式，请输入16进制数据";
                        return;
                    }

                }

            }
            catch
            {

            }

        }
        private void SendButton_Click(object sender, RoutedEventArgs e)// 手动发送数据
        {
            SerialPortSend();
        }
        private void AutoSendCheckBox_Checked(object sender, RoutedEventArgs e)// 设置自动发送定时器
        {
            // 创建定时器
            autoSendTimer.Tick += new EventHandler(AutoSendTimer_Tick);

            // 设置定时时间，开启定时器
            autoSendTimer.Interval = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(autoSendCycleTextBox.Text));
            autoSendTimer.Start();
        }
        private void AutoSendCheckBox_Unchecked(object sender, RoutedEventArgs e)// 关闭自动发送定时器
        {
            autoSendTimer.Stop();
        }
        private void AutoSendTimer_Tick(object sender, EventArgs e)// 自动发送时间到期
        {
            //发送数据
            SerialPortSend();

            //设置新的定时时间           
            autoSendTimer.Interval = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(autoSendCycleTextBox.Text));
        }
        private void ClearSendButton_Click(object sender, RoutedEventArgs e)// 清空发送按钮
        {
            sendTextBox.Clear();
        }
        #endregion

        #region 文件读取与保存 (文件I/O)
        private void FileOpen(object sender, ExecutedRoutedEventArgs e)
        {
            // 打开文件对话框 (默认选择serialCom.txt, 默认格式为文本文档)
            OpenFileDialog openFile = new OpenFileDialog
            {
                FileName = "serialCom",
                DefaultExt = ".txt",
                Filter = "文本文档|*.txt"
            };
            // 如果用户单击确定(选好了文本文档文件)
            if (openFile.ShowDialog() == true)
            {
                // 将文本文档中所有文字读取到发送区
                sendTextBox.Text = File.ReadAllText(openFile.FileName, Encoding.Default);
                // 将文本文档的文件名读取到串口发送面板的文本框中
                fileNameTextBox.Text = openFile.FileName;
            }
        }

        private void FileSave(object sender, ExecutedRoutedEventArgs e)
        {
            // 判断接收区是否有字段
            if (receiveTextBox.Text == string.Empty)
            {
                // 如果没有字段，弹出失败提示
                statusTextBlock.Text = "接收区为空，保存失败。";
            }
            else
            {
                SaveFileDialog saveFile = new SaveFileDialog
                {
                    DefaultExt = ".txt",
                    Filter = "文本文档|*.txt"
                };
                // 如果用户单击确定(确定了文本文档保存的位置和名称)
                if (saveFile.ShowDialog() == true)
                {
                    // 在文本文档中写入当前时间
                    File.AppendAllText(saveFile.FileName, "\r\n******" + DateTime.Now.ToString() + "\r\n******");
                    // 将接收区所有字段写入到文本文档
                    File.AppendAllText(saveFile.FileName, receiveTextBox.Text);
                    // 弹出成功提示
                    statusTextBlock.Text = "保存成功！";
                }
            }
        }

        #endregion

        private void WindowClosed(object sender, ExecutedRoutedEventArgs e)
        {
        }
        private void SendTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 将光标移至文字末尾
            sendTextBox.SelectionStart = sendTextBox.Text.Length;
            if (hexadecimalSendCheckBox.IsChecked == true)
            {
                MatchCollection hexadecimalCollection = Regex.Matches(e.Text, @"[\da-fA-F]");

                foreach (Match mat in hexadecimalCollection)
                {
                    sendTextBox.AppendText(mat.Value);
                }

                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }
        private void CountClearButton_Click(object sender, RoutedEventArgs e)
        {
            //接收、发送计数清零
            receiveBytesCount = 0;
            sendBytesCount = 0;

            //更新数据显示
            statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();
            statusSendByteTextBlock.Text = sendBytesCount.ToString();
        }

        private void StopShowingButton_Checked(object sender, RoutedEventArgs e)
        {
            stopShowingButton.Content = "恢复显示";

        }

        private void StopShowingButton_Unchecked(object sender, RoutedEventArgs e)
        {
            stopShowingButton.Content = "停止显示";

        }

        private void HexadecimalDisplayCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            hexadecimalSendCheckBox.IsChecked = true;
        }

        private void HexadecimalSendCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            hexadecimalDisplayCheckBox.IsChecked = true;
        }

        private void HexadecimalSendCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            hexadecimalDisplayCheckBox.IsChecked = false;

        }

        private void HexadecimalDisplayCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            hexadecimalSendCheckBox.IsChecked = false;

        }
    }





}