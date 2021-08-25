using System;

namespace WpfApp1_TestSoftware_CSMC
{
    internal class UpdateUiBufferDelegate
    {
        private Action<string> showData;

        public UpdateUiBufferDelegate(Action<string> showData)
        {
            this.showData = showData;
        }
    }
}