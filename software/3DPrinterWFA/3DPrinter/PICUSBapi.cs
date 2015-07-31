using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DWORD = System.UInt32;
using PVOID = System.IntPtr;

namespace ControlUSB
{   
    unsafe public class PICUSBapi
    {
        #region Atributos Strings: EndPoint y VID_PID
        string vid_pid_norm = "vid_04D8&pid_0011";
        
        string out_pipe = "\\MCHP_EP1";
        string in_pipe = "\\MCHP_EP1";
        #endregion

        #region Metodos de importacion mpusbapi.dll
        [DllImport("mpusbapi.dll")]
        private static extern DWORD _MPUSBGetDLLVersion();
        [DllImport("mpusbapi.dll")]
        public static extern DWORD _MPUSBGetDeviceCount(string pVID_PID);
        [DllImport("mpusbapi.dll")]
        private static extern void* _MPUSBOpen(DWORD instance, string pVID_PID, string pEP, DWORD dwDir, DWORD dwReserved);
        [DllImport("mpusbapi.dll")]
        private static extern DWORD _MPUSBRead(void* handle, void* pData, DWORD dwLen, DWORD* pLength, DWORD dwMilliseconds);
        [DllImport("mpusbapi.dll")]
        private static extern DWORD _MPUSBWrite(void* handle, void* pData, DWORD dwLen, DWORD* pLength, DWORD dwMilliseconds);
        [DllImport("mpusbapi.dll")]
        private static extern DWORD _MPUSBReadInt(void* handle, DWORD* pData, DWORD dwLen, DWORD* pLength, DWORD dwMilliseconds);
        [DllImport("mpusbapi.dll")]
        private static extern bool _MPUSBClose(void* handle);
        #endregion
        
        #region Metodos para el manejo del mpusbapi.dll
        void* myOutPipe;
        void* myInPipe;

        public void OpenPipes()
        {
            DWORD selection = 0;

            myOutPipe = _MPUSBOpen(selection, vid_pid_norm, out_pipe, 0, 0);
            myInPipe = _MPUSBOpen(selection, vid_pid_norm, in_pipe, 1, 0);
        }

        public void ClosePipes()
        {
            _MPUSBClose(myOutPipe);
            _MPUSBClose(myInPipe);
        }

        private void SendPacket(byte* SendData, DWORD SendLength)
        {
            uint SendDelay = 1000;

            DWORD SentDataLength;

            OpenPipes();
            _MPUSBWrite(myOutPipe, (void*)SendData, SendLength, &SentDataLength, SendDelay);
            ClosePipes();
        }

        private void ReceivePacket(byte* ReceiveData, DWORD* ReceiveLength)
        {
            uint ReceiveDelay = 1000;

            DWORD ExpectedReceiveLength = *ReceiveLength;

            OpenPipes();
            _MPUSBRead(myInPipe, (void*)ReceiveData, ExpectedReceiveLength, ReceiveLength, ReceiveDelay);
            ClosePipes();
        }
        #endregion

        #region Metodos de EnviarDatos y RecibirDatos que llaman a SendPacket y ReceivePacket
        public void EnviarDatos(byte Byte0, byte Byte1, byte Byte2, byte Byte3, byte Byte4, byte Byte5, byte Byte6, byte Byte7, byte Byte8, byte Byte9, byte Byte10, byte Byte11, byte Byte12, byte Byte13, byte Byte14, byte Byte15, byte Byte16, byte Byte17, byte Byte18, byte Byte19)
        {
            byte* Enviar_Buffer = stackalloc byte[20];   //define buffer de 20 bytes para enviar

            Enviar_Buffer[0]  = (byte)Byte0;
            Enviar_Buffer[1]  = (byte)Byte1;
            Enviar_Buffer[2]  = (byte)Byte2;
            Enviar_Buffer[3]  = (byte)Byte3;
            Enviar_Buffer[4]  = (byte)Byte4;
            Enviar_Buffer[5]  = (byte)Byte5;
            Enviar_Buffer[6]  = (byte)Byte6;
            Enviar_Buffer[7]  = (byte)Byte7;
            Enviar_Buffer[8]  = (byte)Byte8;
            Enviar_Buffer[9]  = (byte)Byte9;
            Enviar_Buffer[10] = (byte)Byte10;
            Enviar_Buffer[11] = (byte)Byte11;
            Enviar_Buffer[12] = (byte)Byte12;
            Enviar_Buffer[13] = (byte)Byte13;
            Enviar_Buffer[14] = (byte)Byte14;
            Enviar_Buffer[15] = (byte)Byte15;
            Enviar_Buffer[16] = (byte)Byte16;
            Enviar_Buffer[17] = (byte)Byte17;
            Enviar_Buffer[18] = (byte)Byte18;
            Enviar_Buffer[19] = (byte)Byte19;

            SendPacket(Enviar_Buffer, 20);   //envia el buffer con SendPacket
        }

        public byte RecibirDatos(int i)
        {
            byte* Recibir_Buffer = stackalloc byte[3];  //define buffer de 3 bytes para recibir
            DWORD Longitud = 3;

            ReceivePacket(Recibir_Buffer, &Longitud);

            return Recibir_Buffer[i];
        }
        #endregion
    }
}