using System;

namespace WWShowUSBDeviceTree {
    public class UsbCommonDescriptor
    {
        public int bytes;
        public UsbConfDescType descType;

        /// <summary>
        /// Read Common descriptor
        /// </summary>
        /// <param name="buff">config descriptor buff</param>
        /// <param name="offs">start pos to read</param>
        public UsbCommonDescriptor(byte [] buff, int offs) {
            bytes = buff[offs];
            int cdt = buff[offs+1];

            if (Enum.IsDefined(typeof(UsbConfDescType), cdt)) {
                descType = (UsbConfDescType)cdt;
            } else {
                descType = UsbConfDescType.Unknown;
            }
        }
    }
}
