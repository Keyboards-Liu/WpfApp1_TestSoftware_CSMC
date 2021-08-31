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
            portNameComboBox.SelectedIndex = 0;
        }
        /// <summary>
        /// 在串口运行时进行串口检测和更改
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
                // 接收窗口自动清空
                if (autoClearCheckBox.IsChecked == true)
                {
                    receiveTextBox.Clear();
                }
                receiveTextBox.AppendText(DateTime.Now.ToString() + " <-- " + receiveText + "\r\n");
                // 接收文本解析面板写入
                try
                {
                    // 帧头
                    frameHeader.Text = receiveText.Substring(0 * 3, 1 * 3 - 1);
                    // 长度域
                    frameLength.Text = receiveText.Substring((0 + 1) * 3, 1 * 3 - 1);
                    // 命令域
                    frameCommand.Text = receiveText.Substring((0 + 1 + 1) * 3, 2 * 3 - 1);
                    // 数据地址域
                    frameAddress.Text = receiveText.Substring((0 + 1 + 1 + 2) * 3, 2 * 3 - 1);
                    // 数据内容域
                    frameContent.Text = receiveText.Substring((0 + 1 + 1 + 2 + 2) * 3, (Convert.ToInt32(frameLength.Text, 16) - 2) * 3 - 1);
                    // 校验码
                    frameCRC.Text = receiveText.Substring(receiveText.Length - 2, 2);
                }
                catch { }
                // 仪表参数解析面板写入
                try
                {
                    // 字符串校验
                    string j = "";
                    string[] hexvalue = receiveText.Trim().Split(' ');
                    // 求字符串异或值
                    foreach (string hex in hexvalue) j = HexStrXor(j, hex);
                    if (j == frameHeader.Text)
                    {
                        resCRC.Text = "通过";
                        switch (receiveText.Substring(0, 2))
                        {
                            case "FE":
                                {
                                    // 通信协议
                                    resProtocol.Text = "ZigBee";
                                    // 网络地址
                                    resAddress.Text = frameAddress.Text;
                                    // 厂商号
                                    resVendor.Text = "中国石化";
                                    // 仪表类型
                                    {
                                        if (frameContent.Text.Substring(12, 5) == "00 02") resType.Text = "压力型";
                                        else if (frameContent.Text.Substring(12, 5) == "00 03") resType.Text = "温度型";
                                        else resType.Text = "未知类型";
                                    }
                                    // 仪表组号
                                    resGroup.Text = frameContent.Text.Substring(18, 5);
                                    // 数据类型
                                    resDataType.Text = frameContent.Text.Substring(24, 5);
                                    // 通信成功率
                                    resSucRate.Text = frameContent.Text.Substring(30, 2);
                                    // 电池电压
                                    resBatVol.Text = frameContent.Text.Substring(33, 2);
                                    // 休眠时间
                                    resSleepTime.Text = frameContent.Text.Substring(36, 5);
                                    // 仪表状态
                                    resStatue.Text = frameContent.Text.Substring(42, 5);
                                    // 实时数据
                                    resData.Text = frameContent.Text.Substring(48, 11);

                                }
                                break;
                            default:
                                resProtocol.Text = "未知";
                                resAddress.Foreground = new SolidColorBrush(Colors.Red);
                                break;
                        }
                    }
                    else
                    {
                        // 清空解析面板
                        resProtocol.Clear(); resAddress.Clear(); resVendor.Clear();
                        resType.Clear(); resGroup.Clear(); resDataType.Clear();
                        resSucRate.Clear(); resBatVol.Clear(); resSleepTime.Clear();
                        resStatue.Clear(); resData.Clear(); resCRC.Clear();
                        resCRC.Text = "未通过";
                        resCRC.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
                catch
                {

                }

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
            sendTextBox.Text = sendTextBox.Text.Replace(" ", "");
            sendTextBox.Text = string.Join(" ", Regex.Split(sendTextBox.Text, "(?<=\\G.{2})(?!$)"));
            sendTextBox.SelectionStart = sendTextBox.Text.Length;
            e.Handled = true;
        }
        //private void SendTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (Key.V == e.Key && Keyboard.Modifiers == ModifierKeys.Control)
        //    {
        //        bool bIsPasteOperation = true;
        //    if (true == bIsPasteOperation)
        //    {
        //        // 每输入两个字符自动添加空格
        //        try
        //    {
        //        sendTextBox.Text = sendTextBox.Text.Replace(" ", "");
        //        sendTextBox.Text = string.Join(" ", Regex.Split(sendTextBox.Text, "(?<=\\G.{2})(?!$)"));
        //        sendTextBox.SelectionStart = sendTextBox.Text.Length;
        //        e.Handled = true;
        //    }
        //    catch { }
        //    }
        //    bIsPasteOperation = false;
        //    }
        //}

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
            // 去掉十六进制前缀
            sendTextBox.Text.Replace("0x", "");
            sendTextBox.Text.Replace("0X", "");
            string sendData = sendTextBox.Text;
            // 十六进制数据发送
            try
            {

                // 分割字符串
                string[] strArray = sendData.Split(new char[] { ' ' });
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


        public static string HexStrXor(string HexStr1, string HexStr2)
        {
            //两个十六进制字符串的长度和长度差的绝对值以及异或结果
            int iHexStr1Len = HexStr1.Length;
            int iHexStr2Len = HexStr2.Length;
            int iGap, iHexStrLenLow;
            string result = string.Empty;

            //获取这两个十六进制字符串长度的差值
            iGap = iHexStr1Len - iHexStr2Len;

            //获取这两个十六进制字符串长度最小的那一个
            iHexStrLenLow = iHexStr1Len < iHexStr2Len ? iHexStr1Len : iHexStr2Len;

            //将这两个字符串转换成字节数组
            byte[] bHexStr1 = HexStrToBytes(HexStr1);
            byte[] bHexStr2 = HexStrToBytes(HexStr2);

            /**
             * 把这两个十六进制字符串输出到控制台
             * Console.WriteLine("HexStr1=[{0}]", HexStr1);
             * Console.WriteLine("HexStr2=[{0}]", HexStr2);
             */

            int i = 0;
            //先把每个字节异或后得到一个0~15范围内的整数，再转换成十六进制字符
            for (; i < iHexStrLenLow; ++i)
            {
                result += (bHexStr1[i] ^ bHexStr2[i]).ToString("X");
            }

            result += iGap >= 0 ? HexStr1.Substring(i, iGap) : HexStr2.Substring(i, -iGap);
            return result;
        }
        //将16进制字符串转换成字节数组
        public static byte[] HexStrToBytes(string HexStr)
        {
            if (HexStr == null)
            {
                throw new ArgumentNullException(nameof(HexStr));
            }

            byte[] Bytes = new byte[HexStr.Length];
            try
            {
                for (int i = 0; i < Bytes.Length; ++i)
                {
                    //将每个16进制字符转换成对应的1个字节
                    Bytes[i] = Convert.ToByte(HexStr.Substring(i, 1), 16);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
                /**
                 * 把错误信息输出到控制台
                 * Console.WriteLine("Exception {0} thrown.", e.GetType().FullName);
                 * Console.WriteLine("Message:{0}", e.Message);
                 */
            }
            return Bytes;
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