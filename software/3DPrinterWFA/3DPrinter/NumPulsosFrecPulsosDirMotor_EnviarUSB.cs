using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlUSB
{
    public class NumPulsosFrecPulsosDirMotor_EnviarUSB
    {
        PICUSBapi USBapi = new PICUSBapi();
        private byte[] Enviar_Byte = new byte[5];

        public void NumFrecDirUSB(double NumPulsos, double FrecPulsos, byte DirMotor)
        {
            Enviar_Byte[0] = (byte)NumPulsos;
            Enviar_Byte[1] = (byte)(FrecPulsos*0.5);
            Enviar_Byte[2] = (byte)DirMotor;
            Enviar_Byte[3] = 0;
            Enviar_Byte[4] = 0;            
            this.USBapi.EnviarDatos(Enviar_Byte[0], Enviar_Byte[1], Enviar_Byte[2], Enviar_Byte[3], Enviar_Byte[4]);    
        }

    }
}
