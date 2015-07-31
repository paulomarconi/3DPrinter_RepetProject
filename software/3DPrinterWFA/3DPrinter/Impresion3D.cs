using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace ControlUSB
{
    public class Impresion3D 
    {
        private string [] Gcodes; 
        public int PosicionLinea = 0;                              

        public Vector5D PosicionInicial = new Vector5D(0.0m, 0.0m, 0.0m, 0.0m, 0.0m);
        public Vector5D PosicionFinal = new Vector5D(0.0m , 0.0m, 0.0m, 0.0m, 0.0m);
        public Vector5D Delta = new Vector5D(0.0m, 0.0m, 0.0m, 0.0m, 0.0m);     //delta = numPulsos
        public Vector5D Periodo = new Vector5D(0.0m, 0.0m, 0.0m, 0.0m, 0.0m);
        public Vector5D DirMotor = new Vector5D(0.0m, 0.0m, 0.0m, 0.0m, 0.0m);
        
        private delegate void PrintCommandHandler(string args);
        private Dictionary<int, PrintCommandHandler> GCodeHandlers = new Dictionary<int, PrintCommandHandler>();

        #region SetUpGcodeHandlers + IniciarImpresion + Reset + EndOfDocument + Step
        public void SetUpGcodeHandlers()        
        {
            GCodeHandlers[1] = ControlledMove;
            GCodeHandlers[92] = SetPosition;
            GCodeHandlers[28] = GoHome;
            // gcodes
            /*
            GCodeHandlers[0]  = RapidMove;
            GCodeHandlers[1]  = ControlledMove;
            GCodeHandlers[28] = GoHome;
            GCodeHandlers[4]  = Dwell;
            GCodeHandlers[20] = SetImperial;
            GCodeHandlers[21] = SetMetric;
            GCodeHandlers[90] = AbsolutePositioning;
            GCodeHandlers[91] = IncrementalPositioning;
            GCodeHandlers[92] = SetPosition;

            // mcodes
            MCodeHandlers[0]   = ShutDown;
            MCodeHandlers[140] = SetBedTemp;
            MCodeHandlers[113] = SetExtruderPot;
            MCodeHandlers[109] = SetExtruderTempWait;
            MCodeHandlers[107] = CoolerOff;
            MCodeHandlers[106] = CoolerOn;
            MCodeHandlers[104] = SetExtruderTemp;
            MCodeHandlers[105] = GetExtruderTemp;
            MCodeHandlers[110] = NewPrint;
            MCodeHandlers[108] = ExtruderSpeed;
            MCodeHandlers[101] = ExtruderOn;
            MCodeHandlers[103] = ExtruderOff;
            MCodeHandlers[141] = SetChamberTemp;
            MCodeHandlers[142] = SetHoldingPressure;
            */
        }

        public void IniciarImpresion(string[] gcodes)
        {
            Gcodes = gcodes;

            if (EndOfDocument == true)
            {
                MessageBox.Show("Fin del documento Gcode ");
                return;
            }
            else
            {
                Step("");
            }
        }

        public bool EndOfDocument
        {
            get { return PosicionLinea >= Gcodes.Length; }
        }

        public void Reset()
        {
            PosicionLinea = 0;
            this.PosicionInicial = new Vector5D(0.0m, 0.0m, 0.0m, 0.0m, 0.0m);
            this.PosicionFinal = new Vector5D(0.0m, 0.0m, 0.0m, 0.0m, 0.0m);
            this.Delta = new Vector5D(0.0m, 0.0m, 0.0m, 0.0m, 0.0m);             
        }        
        
        public void Step(string Gcode)
        {
            //-- verifica el doc Gcode cargado o la instruccion Gcode enviada manualmente
            string instr;
            if (Gcode != "")
            {
                instr = Gcode;
            }
            else
            {
                //-- obtener siguiente linea de instruccion
                instr = Gcodes[PosicionLinea++];
            }
            //-- buscar Gcode
            Match match = Regex.Match(instr, @"^G(?<code>\d+)\b(?<args>.*)");
            if (match.Groups["code"].ToString() != "")
            {
                int code = Convert.ToInt32(match.Groups["code"].ToString());
                Debug.Assert(GCodeHandlers.ContainsKey(code), "Unknown G-Code: " + code.ToString());
                if (GCodeHandlers.ContainsKey(code))
                {
                    string args = match.Groups["args"].ToString();
                    GCodeHandlers[code](args);  //llama a la funcion GCodeHandlers[1],[2], etc
                }
                return;
            }
        }
        #endregion

        #region ProcesarCampoNumerico + Escalar + PromedirarPeriodo + DecidirDirMotor
        private void ProcesarCampoNumerico(string args, string field, ref decimal val)
        {
            Match match = Regex.Match(args, field + @"(?<val>[0-9+-.]+\b)");
            if (match.Groups["val"].ToString() != "")
                val = decimal.Round(Convert.ToDecimal(match.Groups["val"].ToString(), CultureInfo.CreateSpecificCulture("en-US")),0);  
        }
        
        private void Escalar(ref Vector5D Fin, Vector5D Ini)
        {
            if (Fin.X != Ini.X) Fin.X = decimal.Round(Fin.X * 3.03m);      //1[mm] = 3.03[pulsos]
            if (Fin.Y != Ini.Y) Fin.Y = decimal.Round(Fin.Y * 10m);        //1[mm] = 10[pulsos]
            if (Fin.Z != Ini.Z) Fin.Z = decimal.Round(Fin.Z * 50m);        //1[mm] = 50[pulsos]
            if (Fin.E != Ini.E) Fin.E = decimal.Round(Fin.E * 70m);        //1[mm] = 70[pulsos]
            if (Fin.F != Ini.F) Fin.F = decimal.Round(2m * Convert.ToDecimal(Math.Pow(2, Convert.ToDouble(-Fin.F * 2m / (60m * 20m)))) * 20m);   //10[mm/s] = 20[ms]              
        }

        private void PromediarPeriodo(ref Vector5D T, Vector5D d, decimal f)     //   0.5, porque OSA toma PeriodoPulsos como semiPeriodoPulsos            
        {
            if ((d.E == 0) && (d.Y == 0) && (d.X == 0))
            {
                T.E = T.Y = T.X = 0;
            }
            if ((d.E == 0) && (d.Y == 0) && (d.X != 0))
            {
                T.X = decimal.Round(3.3m * 7m * f * 0.5m);
            }
            if ((d.E == 0) && (d.Y != 0) && (d.X == 0))
            {
                T.Y = decimal.Round(7m * f * 0.5m);
            }
            if ((d.E == 0) && (d.Y != 0) && (d.X != 0))
            {
                T.Y = decimal.Round(7m * f * 0.5m);
                T.X = decimal.Round((d.Y / d.X) * T.Y);
            }
            if ((d.E != 0) && (d.Y == 0) && (d.X == 0))
            {
                T.E = decimal.Round(f * 0.5m);
            }
            if ((d.E != 0) && (d.Y == 0) && (d.X != 0))
            {
                T.E = decimal.Round(f * 0.5m);
                T.X = decimal.Round((d.E / d.X) * f * 0.5m);
            }
            if ((d.E != 0) && (d.Y != 0) && (d.X == 0))
            {
                T.E = decimal.Round(f * 0.5m);
                T.Y = decimal.Round((d.E / d.Y) * f * 0.5m);
            }
            if ((d.E != 0) && (d.Y != 0) && (d.X != 0))
            {
                T.E = decimal.Round(f * 0.5m);
                T.Y = decimal.Round((d.E / d.Y) * f * 0.5m);
                T.X = decimal.Round((d.E / d.X) * f * 0.5m);                
            }
            
            if (d.Z != 0) T.Z = decimal.Round(7m * f * 0.5m);
            if (d.Z == 0) T.Z = 0;
        }       

        private void DecidirDirMotor(ref Vector5D dir, Vector5D Fin, Vector5D Ini)
        {
            if (Fin.X > Ini.X) dir.X = 128;
            if (Fin.X < Ini.X) dir.X = 64;
            if (Fin.Y > Ini.Y) dir.Y = 32;
            if (Fin.Y < Ini.Y) dir.Y = 16;
            if (Fin.Z > Ini.Z) dir.Z = 8;
            if (Fin.Z < Ini.Z) dir.Z = 4;
            if (Fin.E > Ini.E) dir.E = 2;
            if (Fin.E < Ini.E) dir.E = 1;
        }
        #endregion

        #region Funciones para los comandos Gcode
        private void ControlledMove(string args)
        {
            ProcesarCampoNumerico(args, "X", ref this.PosicionFinal.X);
            ProcesarCampoNumerico(args, "Y", ref this.PosicionFinal.Y);
            ProcesarCampoNumerico(args, "Z", ref this.PosicionFinal.Z);
            ProcesarCampoNumerico(args, "E", ref this.PosicionFinal.E);
            ProcesarCampoNumerico(args, "F", ref this.PosicionFinal.F);
            
            Escalar(ref this.PosicionFinal, this.PosicionInicial);
            
            this.Delta = Vector5D.Abs(this.PosicionFinal - this.PosicionInicial);

            PromediarPeriodo(ref this.Periodo, this.Delta, this.PosicionFinal.F);

            DecidirDirMotor(ref this.DirMotor, this.PosicionFinal, this.PosicionInicial);            

            this.PosicionInicial = this.PosicionFinal;           
        }

        private void SetPosition(string args)
        {
            ProcesarCampoNumerico(args, "X", ref this.PosicionFinal.X);
            ProcesarCampoNumerico(args, "Y", ref this.PosicionFinal.Y);
            ProcesarCampoNumerico(args, "Z", ref this.PosicionFinal.Z);
            ProcesarCampoNumerico(args, "E", ref this.PosicionFinal.E);
            ProcesarCampoNumerico(args, "F", ref this.PosicionFinal.F);

            this.PosicionInicial = this.PosicionFinal; 
        }

        private void GoHome(string args)
        {
            this.PosicionFinal.X = 0;
            this.PosicionFinal.Y = 0;
            this.PosicionFinal.Z = 0;
            this.PosicionFinal.E = 0;
            this.PosicionFinal.F = 0;
        }
        #endregion
    }
}
