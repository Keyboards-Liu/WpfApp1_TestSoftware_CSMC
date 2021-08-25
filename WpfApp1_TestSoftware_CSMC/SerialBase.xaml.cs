﻿using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        #region 内部变量定义
        private SerialPort serialPort = new SerialPort();
        private string receiveData;
        private DispatcherTimer autoSendTimer = new DispatcherTimer();
        private DispatcherTimer autoDetectionTimer = new DispatcherTimer();
        private Encoding setEncoding = Encoding.Default;

        public static uint receiveBytesCount = 0;
        public static uint sendBytesCount = 0;
        #endregion

        #region 串口初始化/串口变更检测
        /// <summary>
        /// 串口初始化
        /// </summary>
        public SerialBase()
        {
            // 初始化组件
            InitializeComponent();
            // 检测和添加串口
            AddPortName();
            // 开启串口检测定时器，并设置自动检测100毫秒1次
            autoDetectionTimer.Tick += new EventHandler(AutoDetectionTimer_Tick);
            autoDetectionTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            autoDetectionTimer.Start();
            // 设置状态栏提示
            statusTextBlock.Text = "准备就绪";
        }
        /// <summary>
        /// 在初始化串口时进行串口检测和添加
        /// </summary>
        private void AddPortName()
        {
            // 检测有效串口，去掉重复串口
            string[] serialPortName = SerialPort.GetPortNames().Distinct().ToArray();
            // 在有效串口号中遍历当前打开的串口号
            foreach (string name in serialPortName)
            {
                // 如果检测到的串口不存在于portNameComboBox中，则添加
                if (portNameComboBox.Items.Contains(name) == false)
                {
                    portNameComboBox.Items.Add(name);
                }
            }
        }
        /// <summary>
        /// 在打开串口时进行串口检测和更改
        /// </summary>
        /// <param name="sender">事件源的对象</param>
        /// <param name="e">事件数据的对象</param>
        private void AutoDetectionTimer_Tick(object sender, EventArgs e)
        {
            // 检测有效串口，去掉重复串口
            string[] serialPortName = SerialPort.GetPortNames().Distinct().ToArray();
            if (turnOnButton.IsChecked == true)
            {
                // 在有效串口号中遍历当前打开的串口号
                foreach (string name in serialPortName)
                {
                    // 如果找到串口，说明串口仍然有效，跳出循环
                    if (serialPort.PortName == name)
                        return;
                }
                // 如果找不到, 说明串口失效了，关闭串口并移除串口名
                turnOnButton.IsChecked = false;
                portNameComboBox.Items.Remove(serialPort.PortName);
                portNameComboBox.SelectedIndex = 0;
                // 输出提示信息
                statusTextBlock.Text = "串口已失效";
            }
            else
            {
                // 检查有效串口和ComboBox中的串口号个数是否不同
                if (portNameComboBox.Items.Count != serialPortName.Length)
                {
                    // 串口数不同，清空ComboBox
                    portNameComboBox.Items.Clear();
                    // 重新添加有效串口
                    foreach (string name in serialPortName)
                    {
                        portNameComboBox.Items.Add(name);
                    }
                    portNameComboBox.SelectedIndex = -1;
                    // 输出提示信息

                    statusTextBlock.Text = "串口列表已更新！";
                }
            }

        }
        #endregion

        #region 打开/关闭串口
        /// <summary>
        /// 串口配置面板
        /// </summary>
        /// <param name="state">使能状态</param>
        private void SerialSettingControlState(bool state)
        {
            // state状态为true时, ComboBox不可用, 反之可用
            portNameComboBox.IsEnabled = state;
            baudRateComboBox.IsEnabled = state;
            parityComboBox.IsEnabled = state;
            dataBitsComboBox.IsEnabled = state;
            stopBitsComboBox.IsEnabled = state;
        }
        /// <summary>
        /// 打开串口按钮
        /// </summary>
        /// <param name="sender">事件源的对象</param>
        /// <param name="e">事件数据的对象</param>
        private void TurnOnButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取面板中的配置, 并设置到串口属性中
                serialPort.PortName = portNameComboBox.Text;
                serialPort.BaudRate = Convert.ToInt32(baudRateComboBox.Text);
                serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), parityComboBox.Text);
                serialPort.DataBits = Convert.ToInt16(dataBitsComboBox.Text);
                serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBitsComboBox.Text);
                serialPort.Encoding = setEncoding;
                // 添加串口事件处理, 设置委托
                serialPort.DataReceived += new SerialDataReceivedEventHandler(ReceiveData);
                // 关闭串口配置面板, 开启串口, 变更按钮文本, 打开绿灯, 显示提示文字
                SerialSettingControlState(false);
                serialPort.Open();
                statusTextBlock.Text = "串口已开启";
                serialPortStatusEllipse.Fill = Brushes.Green;
                turnOnButton.Content = "关闭串口";
            }
            catch
            {
                // 异常时显示提示文字
                statusTextBlock.Text = "开启串口出错！";
            }
        }
        /// <summary>
        /// 关闭串口按钮
        /// </summary>
        /// <param name="sender">事件源的对象</param>
        /// <param name="e">事件数据的对象</param>
        private void TurnOnButton_Unchecked(object sender, RoutedEventArgs e)// 关闭串口
        {
            try
            {
                // 关闭端口, 关闭自动发送定时器, 使能串口配置面板, 变更按钮文本, 关闭绿灯, 显示提示文字 
                serialPort.Close();
                autoSendTimer.Stop();
                SerialSettingControlState(true);
                statusTextBlock.Text = "串口已关闭";
                serialPortStatusEllipse.Fill = Brushes.Gray;
                turnOnButton.Content = "打开串口";
            }
            catch
            {
                // 异常时显示提示文字
                statusTextBlock.Text = "关闭串口出错！";
            }
        }
        #endregion

        #region 串口数据接收
        /// <summary>
        /// 定义全局委托, 用于接收并显示数据
        /// </summary>
        /// <param name="text">输入将要显示的字符串</param>
        private delegate void UpdateUiTextDelegate(string text);
        /// <summary>
        /// 接收串口数据, 并按设置进行进制转换
        /// </summary>
        /// <param name="sender">事件源的对象</param>
        /// <param name="e">事件数据的对象</param>
        private void ReceiveData(object sender, SerialDataReceivedEventArgs e)
        {
            // 多线程同步操作
            Dispatcher.Invoke(new Action(delegate
            {
                // 如果需要接收十进制数据, 明天从这里改起
                if (hexadecimalDisplayCheckBox.IsChecked == false)
                {
                    byte[] receiveBuffer = new byte[serialPort.BytesToRead];
                    serialPort.Read(receiveBuffer, 0, receiveBuffer.Length);
                    for (int i = 0; i < receiveBuffer.Length; i++)
                    {
                        Console.Write(receiveBuffer[i] + " ");
                    }
                    receiveData = serialPort.ReadExisting();
                    Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(ShowData), receiveData);
                }
                // 如果需要显示十六进制数据
                else
                {
                    byte[] receiveBuffer = new byte[serialPort.BytesToRead];
                    serialPort.Read(receiveBuffer, 0, receiveBuffer.Length);
                    for (int i = 0; i < receiveBuffer.Length; i++)
                    {
                        Console.Write(receiveBuffer[i] + " ");
                    }
                    // 字符串转换为十六进制字符串
                    string receiveData = string.Empty;
                    Console.WriteLine();
                    for (int i = 0; i < receiveBuffer.Length; i++)
                    {
                        receiveData += string.Format("{0:X2} ", receiveBuffer[i]);
                        Console.WriteLine(receiveData);
                    }
                    Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(ShowData), receiveData);
                }
            }));
        }

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
            if (!serialPort.IsOpen)
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
                    serialPort.Write(sendData);

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

                        serialPort.Write(sendBuffer, 0, sendBuffer.Length);

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
                sendTextBox.Text = File.ReadAllText(openFile.FileName, setEncoding);
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