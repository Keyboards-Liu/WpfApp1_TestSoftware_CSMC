using Microsoft.Win32;
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
                // 清空缓冲区
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
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

        #region 串口数据接收处理/窗口显示清空功能
        /// <summary>
        /// 定义全局委托, 用于接收并显示数据
        /// </summary>
        /// <param name="text">输入将要显示的字符串</param>
        private delegate void UpdateUiTextDelegate(string text);
        /// <summary>
        /// 接收串口数据, 并转换为16进制字符串
        /// </summary>
        /// <param name="sender">事件源的对象</param>
        /// <param name="e">事件数据的对象</param>
        private void ReceiveData(object sender, SerialDataReceivedEventArgs e)
        {
            // 读取缓冲区内所有字节
            byte[] receiveBuffer = new byte[serialPort.BytesToRead];
            serialPort.Read(receiveBuffer, 0, receiveBuffer.Length);
            // 字符串转换为十六进制字符串
            receiveData = string.Empty;
            Console.WriteLine();
            for (int i = 0; i < receiveBuffer.Length; i++)
            {
                receiveData += string.Format("{0:X2} ", receiveBuffer[i]);
            }
            receiveData = receiveData.Trim();
            // 多线程安全更新页面显示 (Invoke方法暂停工作线程, BeginInvoke方法不暂停)
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(ShowData), receiveData);
        }
        /// <summary>
        /// 接收窗口显示功能
        /// </summary>
        /// <param name="receiveText">需要窗口显示的字符串</param>
        private void ShowData(string receiveText)
        {
            // 更新接收字节数
            receiveBytesCount += (uint)((receiveText.Length + 1) / 3);
            statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();
            // 在接收窗口中显示字符串
            if (receiveText.Length >= 0)
            {
                receiveTextBox.AppendText(DateTime.Now.ToString() + " <-- " + receiveText + "\r\n");
                try
                {
                frameHeader.Text = receiveText.Substring(0 * 3, 1 * 3 - 1);
                frameLength.Text = receiveText.Substring((0 + 1) * 3, 1 * 3 - 1);
                frameCommand.Text = receiveText.Substring((0 + 1 + 1) * 3, 2 * 3 - 1);
                frameAddress.Text = receiveText.Substring((0 + 1 + 1 + 2) * 3, 2 * 3 - 1);
                frameContent.Text = receiveText.Substring((0 + 1 + 1 + 2 + 2) * 3, (Convert.ToInt32(frameLength.Text, 16) - 2) * 3 - 1);
                frameCRC.Text = receiveText.Substring(receiveText.Length - 2, 2);
                }
                catch { }
            }
        }
        /// <summary>
        /// 接收窗口清空按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearReceiveButton_Click(object sender, RoutedEventArgs e)
        {
            receiveTextBox.Clear();
        }
        /// <summary>
        /// 接收窗口自动清空功能
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceiveTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (receiveTextBox.LineCount >= 50 && autoClearCheckBox.IsChecked == true)
            {
                receiveTextBox.Clear();
            }
            else
            {
                receiveScrollViewer.ScrollToEnd();
            }
        }

        #endregion

        #region 串口数据发送/定时发送/窗口清空功能
        /// <summary>
        /// 在发送窗口中写入数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 将光标移至文字末尾
            sendTextBox.SelectionStart = sendTextBox.Text.Length;
            //MatchCollection hexadecimalCollection = Regex.Matches(e.Text, @"[(0(X|x))?\da-fA-F]");
            MatchCollection hexadecimalCollection = Regex.Matches(e.Text, @"[\da-fA-F]");
            foreach (Match mat in hexadecimalCollection)
            {
                sendTextBox.AppendText(mat.Value);
            }
            // 每输入两个字符自动添加空格
            if (sendTextBox.Text.Length % 3 == 2)
            {
                sendTextBox.Text += " ";
                sendTextBox.SelectionStart = sendTextBox.Text.Length;
            }
            e.Handled = true;
        }
        /// <summary>
        /// 串口数据发送逻辑
        /// </summary>
        private void SerialPortSend()
        {
            if (!serialPort.IsOpen)
            {
                statusTextBlock.Text = "请先打开串口！";
                return;
            }
            string sendData = sendTextBox.Text;
            // 十六进制数据发送
            try
            {
                // 去掉十六进制前缀
                sendData.Replace("0x", "");
                sendData.Replace("0X", "");
                // 分割字符串
                string[] strArray = sendData.Split(new char[] { ',', '，', '\r', '\n', ' ', '\t' });
                // 写入数据缓冲区
                byte[] sendBuffer = new byte[strArray.Length];
                int i = 0;
                foreach (string str in strArray)
                {
                    try
                    {
                        int j = Convert.ToInt16(str, 16);
                        sendBuffer[i] = Convert.ToByte(j);
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
            catch
            {
                autoSendCheckBox.IsChecked = false;// 关闭自动发送
                statusTextBlock.Text = "当前为16进制发送模式，请输入16进制数据";
                return;
            }
        }
        /// <summary>
        /// 手动单击按钮发送
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend();
        }
        /// <summary>
        /// 自动发送开启
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoSendCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // 创建定时器
            autoSendTimer.Tick += new EventHandler(AutoSendTimer_Tick);
            // 设置定时时间，开启定时器
            autoSendTimer.Interval = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(autoSendCycleTextBox.Text));
            autoSendTimer.Start();
        }
        /// <summary>
        /// 在每个自动发送周期执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoSendTimer_Tick(object sender, EventArgs e)
        {
            // 发送数据
            SerialPortSend();
            // 设置新的定时时间           
            autoSendTimer.Interval = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(autoSendCycleTextBox.Text));
        }
        /// <summary>
        /// 自动发送关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoSendCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            autoSendTimer.Stop();
        }
        /// <summary>
        /// 清空发送区
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearSendButton_Click(object sender, RoutedEventArgs e)
        {
            sendTextBox.Clear();
        }
        #endregion

        #region 文件读取与保存 (文件I/O)
        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <summary>
        /// 读取接收区并保存文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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


        private void CountClearButton_Click(object sender, RoutedEventArgs e)
        {
            //接收、发送计数清零
            receiveBytesCount = 0;
            sendBytesCount = 0;

            //更新数据显示
            statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();
            statusSendByteTextBlock.Text = sendBytesCount.ToString();
        }



        //private void StopShowingButton_Checked(object sender, RoutedEventArgs e)
        //{
        //    stopShowingButton.Content = "恢复显示";
        //}

        //private void StopShowingButton_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    stopShowingButton.Content = "停止显示";
        //}

        //private void HexadecimalDisplayCheckBox_Checked(object sender, RoutedEventArgs e)
        //{
        //    hexadecimalSendCheckBox.IsChecked = true;
        //}

        //private void HexadecimalSendCheckBox_Checked(object sender, RoutedEventArgs e)
        //{
        //    hexadecimalDisplayCheckBox.IsChecked = true;
        //}

        //private void HexadecimalSendCheckBox_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    hexadecimalDisplayCheckBox.IsChecked = false;

        //}

        //private void HexadecimalDisplayCheckBox_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    hexadecimalSendCheckBox.IsChecked = false;

        //}
    }





}