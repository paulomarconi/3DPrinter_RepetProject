using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;


namespace ControlUSB
{
    public partial class InterfazMain3DPrinter : Form
    {
        //-- atributos USB        
        private byte[]  Enviar_Byte = new byte[20];
        private byte[]  Recibir_Byte = new byte[3];              
        public string   Nombre = "3D Printer Repet";
        public string   vid_pid = "vid_04D8&pid_0011";

        //-- atributos Monitoreo
        public double ADC = 0;
        public string   Direccion;
        public DateTime tiempoi, tiempof;
        public Int64    Contador = 0;
        public byte     GuardarDocumento = 0;
        //-- objetos
        PICUSBapi USBapi = new PICUSBapi();  
        GcodeDocument Documento = new GcodeDocument();      
        Impresion3D Impresion = new Impresion3D(); 
        //--------------------------------------------------      
        public InterfazMain3DPrinter()
        {
            InitializeComponent();
        }
        
        #region Grupo Metodo Generico: Inicio, StatusDispositivo, Cierre_App sirve para limpiar EnviarBytes
        private void Inicio(object sender, EventArgs e)
        {
            Status_Dispositivo();
        }

        private void Status_Dispositivo()
        {
            uint Cuenta = PICUSBapi._MPUSBGetDeviceCount(vid_pid);
            if (this.WindowState != FormWindowState.Minimized) //Show the average colors on screen
            {
                if (Cuenta == 0)
                {
                    StatusStrip.Text = string.Format("{0} No Conectado", Nombre);
                    StatusStrip.ForeColor = Color.Red;
                }
                else
                {
                    StatusStrip.Text = string.Format("{0} Conectado", Nombre);
                    StatusStrip.ForeColor = Color.Green;
                }
            }
        }

        private void Cierre_App(object sender, FormClosingEventArgs e)
        {
            bool close = false;
            this.TopMost = false;
            
            if (e.CloseReason != CloseReason.WindowsShutDown)
            {
                DialogResult result = MessageBox.Show("Está seguro que desea salir?", string.Format("{0}", Nombre), MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    close = true;
                    Enviar_Data(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                }
            }
            else
            {
                close = true;
            }
            if (close == true)
            {
                // Al cerrar la aplicación, llevo las salidas a 0.
                Enviar_Data(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }
            else
            {
                e.Cancel = true;
                this.TopMost = true;
            }
        }
        #endregion

        #region Metodo Enviar_Data  
        public void Enviar_Data(UInt16 NumPulsosX, UInt16 NumPulsosY, UInt16 NumPulsosZ, UInt16 NumPulsosE, UInt16 PeriodoPulsosX, UInt16 PeriodoPulsosY, byte PeriodoPulsosZ, byte PeriodoPulsosE, byte DirMotorX, byte DirMotorY, byte DirMotorZ, byte DirMotorE, byte GlaGlc, byte PWM)
        {
            Enviar_Byte[0]  = (byte)(NumPulsosX >> 8);  // Se envia en 2 partes para reemsamblarse en el PIC
            Enviar_Byte[1]  = (byte)(NumPulsosX & 0xFF);
            Enviar_Byte[2]  = (byte)(NumPulsosY >> 8);
            Enviar_Byte[3]  = (byte)(NumPulsosY & 0xFF);
            Enviar_Byte[4]  = (byte)(NumPulsosZ >> 8);
            Enviar_Byte[5]  = (byte)(NumPulsosZ & 0xFF);
            Enviar_Byte[6]  = (byte)(NumPulsosE >> 8);
            Enviar_Byte[7]  = (byte)(NumPulsosE & 0xFF);
            Enviar_Byte[8]  = (byte)(PeriodoPulsosX >> 8);
            Enviar_Byte[9]  = (byte)(PeriodoPulsosX & 0xFF);
            Enviar_Byte[10] = (byte)(PeriodoPulsosY >> 8);
            Enviar_Byte[11] = (byte)(PeriodoPulsosY & 0xFF);
            Enviar_Byte[12] = (byte)PeriodoPulsosZ;
            Enviar_Byte[13] = (byte)PeriodoPulsosE;
            Enviar_Byte[14] = (byte)DirMotorX;
            Enviar_Byte[15] = (byte)DirMotorY;
            Enviar_Byte[16] = (byte)DirMotorZ;
            Enviar_Byte[17] = (byte)DirMotorE;
            Enviar_Byte[18] = (byte)GlaGlc;
            Enviar_Byte[19] = (byte)PWM;
            this.USBapi.EnviarDatos(Enviar_Byte[0], Enviar_Byte[1], Enviar_Byte[2], Enviar_Byte[3], Enviar_Byte[4], Enviar_Byte[5], Enviar_Byte[6], Enviar_Byte[7], Enviar_Byte[8], Enviar_Byte[9], Enviar_Byte[10], Enviar_Byte[11], Enviar_Byte[12], Enviar_Byte[13], Enviar_Byte[14], Enviar_Byte[15], Enviar_Byte[16], Enviar_Byte[17], Enviar_Byte[18], Enviar_Byte[19]);
        }
        #endregion

        #region Grupo Metodo Control Manual XYZE
        private void buttonPositivoX_Click_1(object sender, EventArgs e)
        {
            Enviar_Data(Convert.ToUInt16(textBoxNumPulsosX.Text), 0, 0, 0, Convert.ToUInt16(decimal.Round(Convert.ToDecimal(textBoxPeriodoPulsosX.Text) * 0.5m)), 0, 0, 0, 128, 0, 0, 0, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);          
        }
        
        private void buttonNegativoX_Click(object sender, EventArgs e)
        {
            Enviar_Data(Convert.ToUInt16(textBoxNumPulsosX.Text), 0, 0, 0, Convert.ToUInt16(decimal.Round(Convert.ToDecimal(textBoxPeriodoPulsosX.Text) * 0.5m)), 0, 0, 0, 64, 0, 0, 0, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);
        }
        
        private void buttonPositivoY_Click(object sender, EventArgs e)
        {
            Enviar_Data(0, Convert.ToUInt16(textBoxNumPulsosY.Text), 0, 0, 0, Convert.ToUInt16(decimal.Round(Convert.ToDecimal(textBoxPeriodoPulsosY.Text) * 0.5m)), 0, 0, 0, 32, 0, 0, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);
        }

        private void buttonNegativoY_Click(object sender, EventArgs e)
        {
            Enviar_Data(0, Convert.ToUInt16(textBoxNumPulsosY.Text), 0, 0, 0, Convert.ToUInt16(decimal.Round(Convert.ToDecimal(textBoxPeriodoPulsosY.Text) * 0.5m)), 0, 0, 0, 16, 0, 0, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);
        }

        private void buttonPositivoZ_Click(object sender, EventArgs e)
        {
            Enviar_Data(0, 0, Convert.ToUInt16(textBoxNumPulsosZ.Text), 0, 0, 0, Convert.ToByte(decimal.Round(Convert.ToDecimal(textBoxPeriodoPulsosZ.Text) * 0.5m)), 0, 0, 0, 8, 0, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);
        }

        private void buttonNegativoZ_Click(object sender, EventArgs e)
        {
            Enviar_Data(0, 0, Convert.ToUInt16(textBoxNumPulsosZ.Text), 0, 0, 0, Convert.ToByte(decimal.Round(Convert.ToDecimal(textBoxPeriodoPulsosZ.Text) * 0.5m)), 0, 0, 0, 4, 0, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);
        }

        private void buttonPositivoE_Click(object sender, EventArgs e)
        {
            Enviar_Data(0, 0, 0, Convert.ToUInt16(textBoxNumPulsosE.Text), 0, 0, 0, Convert.ToByte(decimal.Round(Convert.ToDecimal(textBoxPeriodoPulsosE.Text) * 0.5m)), 0, 0, 0, 2, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);
        }

        private void buttonNegativoE_Click(object sender, EventArgs e)
        {
            Enviar_Data(0, 0, 0, Convert.ToUInt16(textBoxNumPulsosE.Text), 0, 0, 0, Convert.ToByte(decimal.Round(Convert.ToDecimal(textBoxPeriodoPulsosE.Text) * 0.5m)), 0, 0, 0, 1, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);
        }
        #endregion
        
        #region Metodo Seleccion Gla - Glc
        private void trackBarGlaGlc_Scroll(object sender, EventArgs e)
        {
            Enviar_Data(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);
        }
        #endregion

        #region Metodo Referencia r(t) PWM
        private void hScrollBarPWM_Scroll(object sender, ScrollEventArgs e)
        {
            //int PWM = int.Parse(textBoxDatoPWM.Text);
            labelDatoPWMEnviado.Text = Convert.ToString(hScrollBarPWM.Value * 0.01961);     //conversion; 1V=51; 5[V]/255=0.01961(teórico) || 4.96[V]/255=0.01945(práctico).... (revisar notas) 
            labelDatoTempEnviado.Text = Convert.ToString(hScrollBarPWM.Value * 1.17647);    //conversion; 1V=51; 300[°C]/255=1.17647

            Enviar_Data(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);
        }
        #endregion

        #region Grupo Metodo Monitoreo: Iniciar + Parar + Limpiar + Guardar + TimerMuestreo_Tick
        private void buttonIniciarMuestreo_Click(object sender, EventArgs e)
        {
            TimerMuestreo.Interval = Convert.ToUInt16(textBoxTiempoDeMuestreo.Text);
            TimerMuestreo.Start();
        }

        private void buttonPararMuestreo_Click(object sender, EventArgs e)
        {            
            GuardarDocumento = 0;
            Contador = 0;
            labelNumDeMuestras.Text = "0";
            TimerMuestreo.Stop();         
        }

        private void buttonLimpiarMuestreo_Click(object sender, EventArgs e)
        {            
            chartRvsY.Series["SeriesY"].Points.Clear();
            chartRvsY.Series["SeriesR"].Points.Clear();
        }
        
        private void buttonGuardarMuestreo_Click(object sender, EventArgs e)
        {
            // Configure save file dialog box
            SaveFileDialog dlg1 = new SaveFileDialog();
            dlg1.Title = "Guardar Archivo de datos";
            dlg1.Filter = "Archivo de Texto (.txt) |*.txt";
            dlg1.DefaultExt = "txt";
            dlg1.AddExtension = true;
            dlg1.RestoreDirectory = true;
            
            // Show open file dialog box
            DialogResult result = dlg1.ShowDialog();

            // Process open file dialog box results 
            if (result == DialogResult.OK)     
            {
                string ruta = dlg1.FileName;
                Direccion = ruta;
                TextWriter fichero = new StreamWriter(ruta);
                fichero.WriteLine("Datos de la Planta a lazo abierto (Gla) o lazo cerrado (Glc)");
                fichero.WriteLine(ruta);
                tiempoi = DateTime.Now;
                fichero.WriteLine(tiempoi);
                fichero.WriteLine();
                fichero.WriteLine("{0}          {1}         {2}         {3}", "r(t):Voltaje[V]", "y(t):Voltaje[V]", "y(t):Temp[°C]", "Tiempo de muestreo:tm[ms]");
                fichero.Close();
                MessageBox.Show("Se ha guardado el archivo: " + dlg1.FileName);
                labelNumDeMuestras.Text = "0";

                int intervalo = int.Parse(textBoxTiempoDeMuestreo.Text);
                TimerMuestreo.Interval = intervalo; 

                GuardarDocumento = 1;   
            }
            else
            {
                GuardarDocumento = 0;
                MessageBox.Show("Datos no creados.");                                
            }
            dlg1.Dispose();
            dlg1 = null;            
        }        
        
        private void TimerMuestreo_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < 3; i++)    //con el incrementador llena los bytes "Recibir_Byte" con el contenido de "this.USBAPI.RecibirDatos(i)"
            {
                Recibir_Byte[i] = this.USBapi.RecibirDatos(i);
            }
            ADC = (UInt16)Recibir_Byte[0] * 256 + (UInt16)Recibir_Byte[1];    //rearma el valor de ADC del PIC y pone rango de 0 a 1023
            
            if (GuardarDocumento == 1)
            {
                Contador++;
                TimeSpan duracion = DateTime.Now - tiempoi;
                StreamWriter fichero = new StreamWriter(Direccion, true);
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");   //para WriteLine con la notación "en-US" "23,713.22476" para trabajar directamente en Matlab porque por defecto fue compilado en "es-BO"
                fichero.WriteLine("{0}                      {1}                     {2}                     {3}", Convert.ToString(decimal.Round(Convert.ToDecimal(hScrollBarPWM.Value * 0.01961), 3)), Convert.ToString(decimal.Round(Convert.ToDecimal(ADC * 0.00488), 3)), Convert.ToString(decimal.Round(Convert.ToDecimal(ADC * 0.00488 * 60), 3)), Convert.ToString(decimal.Round(Convert.ToDecimal(duracion.TotalMilliseconds), 3))  );
                fichero.Close();
                labelNumDeMuestras.Text = Convert.ToString(Contador);
            }
            
            progressBarADC.Value = (UInt16)ADC;
            labelValorADC.Text = Convert.ToString(ADC);
            labelValorVoltaje.Text = Convert.ToString(ADC * 0.00488);   //5[V]/1023[bits]=0.00488
            labelValorTemp.Text = Convert.ToString(ADC * 0.00488 * 60); //300[°C]/5[V]=60

            chartRvsY.Series["SeriesR"].Points.AddY(hScrollBarPWM.Value * 0.01961);
            chartRvsY.Series["SeriesY"].Points.AddY(ADC * 0.00488);            
        }
        #endregion

        #region Grupo Metodo Gcode: CargarGcode + Step + buttonStep + TimerStep + Enviar Gcode + ResetStep
        private void buttonCargarGcode_Click(object sender, EventArgs e)
        {               
            // Configure open file dialog box
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "Documento"; // Default file name
            dlg.DefaultExt = ".gcode";  // Default file extension
            dlg.Filter = "Documento Gcode (.gcode)|*.gcode"; // Filter files by extension 

            // Show open file dialog box
            DialogResult result = dlg.ShowDialog();
            
            // Process open file dialog box results 
            if (result == DialogResult.OK)
            {
                // Procesa "dlg.FileName" a traves de la clase "GcodeDocument"
                try
                {
                    this.Documento.Cargar(dlg.FileName);
                    if (this.Documento.GcodeCargado == true)
                    {
                        MessageBox.Show("Gcode cargado correctamente ");
                        labelSizeLinesGcodeDocument.Text = Convert.ToString(this.Documento.Gcodes.Length);
                        return;
                    }
                                       
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar el Gcode: " + ex.Message);
                    return;
                }
            }           
        }

        private void Step()
        {
            if (this.Documento.GcodeCargado == false)
            {
                MessageBox.Show("Gcode no cargado ");
                return;
            }
            else
            {
                this.Impresion.SetUpGcodeHandlers();
                this.Impresion.IniciarImpresion(this.Documento.Gcodes);
                if (this.Impresion.EndOfDocument == false)
                    Enviar_Data(Convert.ToUInt16(this.Impresion.Delta.X), Convert.ToUInt16(this.Impresion.Delta.Y), Convert.ToUInt16(this.Impresion.Delta.Z), Convert.ToUInt16(this.Impresion.Delta.E), Convert.ToUInt16(this.Impresion.Periodo.X), Convert.ToUInt16(this.Impresion.Periodo.Y), Convert.ToByte(this.Impresion.Periodo.Z), Convert.ToByte(this.Impresion.Periodo.E), Convert.ToByte(this.Impresion.DirMotor.X), Convert.ToByte(this.Impresion.DirMotor.Y), Convert.ToByte(this.Impresion.DirMotor.Z), Convert.ToByte(this.Impresion.DirMotor.E), (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);
                else TimerStep.Stop();

                labelLineaActual.Text = Convert.ToString(this.Impresion.PosicionLinea);

                labelPosFinX.Text = Convert.ToString(this.Impresion.PosicionFinal.X);
                labelPosFinY.Text = Convert.ToString(this.Impresion.PosicionFinal.Y);
                labelPosFinZ.Text = Convert.ToString(this.Impresion.PosicionFinal.Z);
                labelPosFinE.Text = Convert.ToString(this.Impresion.PosicionFinal.E);
                labelPosFinF.Text = Convert.ToString(this.Impresion.PosicionFinal.F);
                labelDeltaX.Text = Convert.ToString(this.Impresion.Delta.X);
                labelDeltaY.Text = Convert.ToString(this.Impresion.Delta.Y);
                labelDeltaZ.Text = Convert.ToString(this.Impresion.Delta.Z);
                labelDeltaE.Text = Convert.ToString(this.Impresion.Delta.E);
                labelPeriodoX.Text = Convert.ToString(this.Impresion.Periodo.X);
                labelPeriodoY.Text = Convert.ToString(this.Impresion.Periodo.Y);
                labelPeriodoZ.Text = Convert.ToString(this.Impresion.Periodo.Z);
                labelPeriodoE.Text = Convert.ToString(this.Impresion.Periodo.E);
                labelDeltaPeriodoX.Text = Convert.ToString(this.Impresion.Delta.X * this.Impresion.Periodo.X);
                labelDeltaPeriodoY.Text = Convert.ToString(this.Impresion.Delta.Y * this.Impresion.Periodo.Y);
                labelDeltaPeriodoZ.Text = Convert.ToString(this.Impresion.Delta.Z * this.Impresion.Periodo.Z);
                labelDeltaPeriodoE.Text = Convert.ToString(this.Impresion.Delta.E * this.Impresion.Periodo.E);
                labelDirX.Text = Convert.ToString(this.Impresion.DirMotor.X);
                labelDirY.Text = Convert.ToString(this.Impresion.DirMotor.Y);
                labelDirZ.Text = Convert.ToString(this.Impresion.DirMotor.Z);
                labelDirE.Text = Convert.ToString(this.Impresion.DirMotor.E);
            }
        }

        private void buttonStep_Click(object sender, EventArgs e)
        {
            TimerStep.Stop();
            Step();
        }

        private void buttonTimerStep_Click(object sender, EventArgs e)
        {            
            if (this.Documento.GcodeCargado == false)
            {
                MessageBox.Show("Gcode no cargado ");
                return;
            }
            TimerStep.Start();               
        }

        private void TimerStep_Tick(object sender, EventArgs e)
        {
            if (Recibir_Byte[2] == 0)            
            {
                TimerStep.Interval = Convert.ToUInt16(textBoxTimerStep.Text);
                Step();
            }
            else return;
        }

        private void buttonEnviarGcode_Click(object sender, EventArgs e)
        {
            this.Impresion.SetUpGcodeHandlers();
            this.Impresion.Step(textBoxEnviarGcode.Text);

            Enviar_Data(Convert.ToUInt16(this.Impresion.Delta.X), Convert.ToUInt16(this.Impresion.Delta.Y), Convert.ToUInt16(this.Impresion.Delta.Z), Convert.ToUInt16(this.Impresion.Delta.E), Convert.ToUInt16(this.Impresion.Periodo.X), Convert.ToUInt16(this.Impresion.Periodo.Y), Convert.ToByte(this.Impresion.Periodo.Z), Convert.ToByte(this.Impresion.Periodo.E), Convert.ToByte(this.Impresion.DirMotor.X), Convert.ToByte(this.Impresion.DirMotor.Y), Convert.ToByte(this.Impresion.DirMotor.Z), Convert.ToByte(this.Impresion.DirMotor.E), (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);

            labelPosFinX.Text = Convert.ToString(this.Impresion.PosicionFinal.X);
            labelPosFinY.Text = Convert.ToString(this.Impresion.PosicionFinal.Y);
            labelPosFinZ.Text = Convert.ToString(this.Impresion.PosicionFinal.Z);
            labelPosFinE.Text = Convert.ToString(this.Impresion.PosicionFinal.E);
            labelPosFinF.Text = Convert.ToString(this.Impresion.PosicionFinal.F);
            labelDeltaX.Text = Convert.ToString(this.Impresion.Delta.X);
            labelDeltaY.Text = Convert.ToString(this.Impresion.Delta.Y);
            labelDeltaZ.Text = Convert.ToString(this.Impresion.Delta.Z);
            labelDeltaE.Text = Convert.ToString(this.Impresion.Delta.E);
            labelPeriodoX.Text = Convert.ToString(this.Impresion.Periodo.X);
            labelPeriodoY.Text = Convert.ToString(this.Impresion.Periodo.Y);
            labelPeriodoZ.Text = Convert.ToString(this.Impresion.Periodo.Z);
            labelPeriodoE.Text = Convert.ToString(this.Impresion.Periodo.E);
            labelDeltaPeriodoX.Text = Convert.ToString(this.Impresion.Delta.X * this.Impresion.Periodo.X);
            labelDeltaPeriodoY.Text = Convert.ToString(this.Impresion.Delta.Y * this.Impresion.Periodo.Y);
            labelDeltaPeriodoZ.Text = Convert.ToString(this.Impresion.Delta.Z * this.Impresion.Periodo.Z);
            labelDeltaPeriodoE.Text = Convert.ToString(this.Impresion.Delta.E * this.Impresion.Periodo.E);
            labelDirX.Text = Convert.ToString(this.Impresion.DirMotor.X);
            labelDirY.Text = Convert.ToString(this.Impresion.DirMotor.Y);
            labelDirZ.Text = Convert.ToString(this.Impresion.DirMotor.Z);
            labelDirE.Text = Convert.ToString(this.Impresion.DirMotor.E);
            
            this.Impresion.Reset();
        }

        private void buttonResetStep_Click(object sender, EventArgs e)
        {
            TimerStep.Stop();
            this.Impresion.Reset();
            Enviar_Data(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)trackBarGlaGlc.Value, (byte)hScrollBarPWM.Value);

            labelLineaActual.Text = "0";

            labelPosFinX.Text = "0";
            labelPosFinY.Text = "0";
            labelPosFinZ.Text = "0";
            labelPosFinE.Text = "0";
            labelPosFinF.Text = "0";
            labelDeltaX.Text = "0";
            labelDeltaY.Text = "0";
            labelDeltaZ.Text = "0";
            labelDeltaE.Text = "0";            
            labelPeriodoX.Text = "0";
            labelPeriodoY.Text = "0";
            labelPeriodoZ.Text = "0";
            labelPeriodoE.Text = "0";
            labelDeltaPeriodoX.Text = "0";
            labelDeltaPeriodoY.Text = "0";
            labelDeltaPeriodoZ.Text = "0";
            labelDeltaPeriodoE.Text = "0";
            labelDirX.Text = "0";
            labelDirY.Text = "0";
            labelDirZ.Text = "0";
            labelDirE.Text = "0";
        }
        #endregion        



        private void buttonTest_Click(object sender, EventArgs e)
        {            
            //decimal r = decimal.Round(Convert.ToDecimal("13.224", CultureInfo.CreateSpecificCulture("en-US")),1);
            //labelTest1.Text = Convert.ToString(Math.Round(Convert.ToDouble("23,713.22476", CultureInfo.CreateSpecificCulture("en-US")), 3));            
            //labelTest2.Text = Convert.ToString(decimal.Round(Convert.ToDecimal("23713,22476", CultureInfo.CreateSpecificCulture("es-BO")), 3));
            //-- Nota: Double vs Decimal: Decimal tiene mas precision y menos rango, lo que reduce el consumo de ram
            //--                          , pero Decimal no tiene conversion implicita con int, short, float, como lo tiene Double.

            UInt16 Xpenviado;   //100[mm]*5.88[pulsos/mm]=588[pulsos]
            decimal Xprecibido;
            byte enviaXa,enviaXb;

            Xpenviado = Convert.ToUInt16(textBoxTest.Text);           
            enviaXa = (byte)(Xpenviado >> 8);
            enviaXb = (byte)(Xpenviado & 0xFF);
            
            Xprecibido = (byte)enviaXa * 256 + (byte)enviaXb;
            labelTest2.Text = Convert.ToString(Xprecibido);
        }

        
          

    }
}

