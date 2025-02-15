//--- config: PIC ------------------------------------------------------------
#include <18F4550.h>
#device adc=10
#fuses HSPLL,MCLR,NOWDT,NOPROTECT,NOLVP,NODEBUG,USBDIV,PLL5,CPUDIV1,VREGEN 
#use delay(crystal=20MHz, clock=48MHz) 

//cabecera necesaria para trabajar con el Bootloader "ex_usb_bootloader.c"
#include "usb_bootloader.h"

//--- config: USB bulkmode ---------------------------------------------------
#define USB_HID_DEVICE     FALSE
#define USB_EP1_TX_ENABLE  USB_ENABLE_BULK  //turn on EP1(EndPoint1) 
                                            //for IN bulk/interrupt transfers
#define USB_EP1_RX_ENABLE  USB_ENABLE_BULK  //turn on EP1(EndPoint1) 
                                            //for OUT bulk/interrupt transfers
#define USB_EP1_TX_SIZE    3   //size to allocate for the tx endpoint 1 buffer
#define USB_EP1_RX_SIZE    20  //size to allocate for the rx endpoint 1 buffer

#include <pic18_usb.h>        //Microchip PIC18Fxx5x Hardware layer 
                              //for CCS's PIC USB driver
#include "usb_descriptor.h"   //Configuracion del USB y los descriptores 
                              //para este dispositivo
#include <usb.c>  //handles usb setup tokens and get descriptor reports

#include <osa.h>  //OSA RTOS

//--- config: Directivas I/O -------------------------------------------------
#use standard_io(A)
#use standard_io(B)
#use standard_io(C)
#use standard_io(D)

//--- def: variables Tarea USB -----------------------------------------------
unsigned int8  RecibirByte[20];   //vector [0;19]=20 vectores x 1Byte                
unsigned int8  EnviarByte[3];     //vector [0;2]=3 vectores x 1Byte

//--- def: variables Tareas mPaP ---------------------------------------------
unsigned int16 numPulsosX,numPulsosY,numPulsosZ,numPulsosE;   
unsigned int16 periodoPulsosX,periodoPulsosY;
unsigned int8 periodoPulsosZ,periodoPulsosE;
unsigned int8 dirMotorX,dirMotorY,dirMotorZ,dirMotorE;    
#define dirX    PIN_D0
#define dirY    PIN_D1
#define dirZ    PIN_D2
#define dirE    PIN_D3
#define clockX  PIN_D4 
#define clockY  PIN_D5
#define clockZ  PIN_D6
#define clockE  PIN_D7

//--- def: variables para funciones; ADC PIC, Gla, GlcPID --------------------
unsigned int8 GlaoGlc;
unsigned int16 valorR,valorY,valorYusb,control;
float Kp,Kd,Ki,Kpz,Kiz,Kdz,T;    //Constantes PID  
float r,y,u,e,e1,p,i,i1,d,d1;    //Variables PID
unsigned int16 max,min;          //Variables anti-windup 

//--- def: Funciones Genericas -----------------------------------------------
void Inicio (void);
void ADC(void);

//--- def: Tareas OSA --------------------------------------------------------
void USB(void);
void Gla(void);
void Glc_PIDdiscreto(void);
void MotorX(void);
void MotorY(void);
void MotorZ(void);
void MotorE(void);

//****************************************************************************
//**** main
//****************************************************************************
void main(void){
   
   Inicio();
   
   OS_Init();              // Init OS
   OS_Task_Define(USB);    // Define tasks.
   OS_Task_Define(Gla); 
   OS_Task_Define(Glc_PIDdiscreto);         
   OS_Task_Define(MotorX);
   OS_Task_Define(MotorY);
   OS_Task_Define(MotorZ);        
   OS_Task_Define(MotorE);
   
   // Create tasks. // if 0 = no priorities 
   OS_Task_Create(0, USB);    
   OS_Task_Create(0, Gla);
   OS_Task_Create(0, Glc_PIDdiscreto);      
   OS_Task_Create(0, MotorX);  
   OS_Task_Create(0, MotorY);
   OS_Task_Create(0, MotorZ);  
   OS_Task_Create(0, MotorE);
   
   // Create tasks, Task priority. Allowed values from 0(highest) to 7(lowest)
   /*OS_Task_Create(0, USB);    
   OS_Task_Create(7, Gla);
   OS_Task_Create(1, Glc_PIDdiscreto);      
   OS_Task_Create(2, MotorX);  
   OS_Task_Create(2, MotorY);
   OS_Task_Create(2, MotorZ);  
   OS_Task_Create(2, MotorE);
   
   OS_Bsem_Set(BS_GLAGLC_FREE);*/
   
   OS_EI();                // Enable interrupts
   OS_Run();               // Running scheduler
}

//**** Funciones genericas ***************************************************
void Inicio(void){ 
   //--- ini: mPaP -----------------------------------------------------------
   numPulsosX=numPulsosY=numPulsosZ=numPulsosE=0;
   periodoPulsosX=periodoPulsosY=periodoPulsosZ=periodoPulsosE=0;
   //--- ini: PID ------------------------------------------------------------
   min=0.0; max=1023 ; //valor Anti-windup    
   i1=0;e1=0;d1=0;   
   Kd=8; Kp=8; Ki=0.02915;
   T=5;       //Tiempo de muestreo  tr/6 < T < tr/20
   Kpz=Kp; Kiz=Ki*T/2; Kdz=Kd/T;
   
   //--- ini: CCP ---------------------------------------------------------
   //--- Pre=16 PR2=249 Pos=1, PWMF=3kHz,PWMT=300us con Fosc(clock)=48MHz
   setup_timer_2(T2_DIV_BY_16,249,1); 
   setup_ccp1(ccp_pwm);               //Configurar modulo CCP1 en modo PWM
   set_pwm1_duty(0);  
   
   //--- ini: ADC ---------------------------------------------------------  
   setup_adc_ports(AN0|VSS_VDD );     
   setup_adc(ADC_CLOCK_INTERNAL); 
   //setup_adc(ADC_CLOCK_DIV_8); //respetar el Tad>1.6us 
                                 //Tad=8/Fosc=8/20Mhz=400ns
   set_adc_channel(0);           //Seleccionar Canal(0)=AN0=A0 para ADC
   
   //--- ini: TIMER0 for OS_Timer() ---------------------------------------                                  
   setup_timer_0(RTCC_INTERNAL|RTCC_DIV_1);  //config Timer0, Pre=1=RTCC_DIV_1
   //set_timer0(0xF63B);   //carga del Timer0, clock=20MHz, Fout=1kHz=0xF63B
   set_timer0(0xE88F);     //carga del Timer0, clock=48MHz, Fout=1kHz=0xE88F
   
   //--- ini: Interrupts --------------------------------------------------
   //enable_interrupts(GLOBAL);
   enable_interrupts(INT_TIMER0);            //habilita interrupcion Timer0
   //enable_interrupts(INT_TIMER2);
   
   //--- ini: USB ------------------------------------------------------------
   usb_init();                   //inicializamos el USB
   usb_task();                   //habilita periferico usb e interrupciones
   usb_wait_for_enumeration();   //esperamos hasta que el PicUSB 
                                 //sea configurado por el host
   delay_ms(50);   
}

void ADC(void){
   valorYusb=read_adc();
   //delay_us(1); //Tacq minimo de carga 
                  //del capacitor(sample&hold)=8/Fosc=8/20MHz=400ns=0.4us
                  
   //-- El ADC es de 10 bits y puedo enviar solo 8, asi que separo
   //-- la variable en 2 bytes "EnviarByte[0] y EnviarByte[1]", 
   //-- luego se arma en C# (ver notas)
   EnviarByte[0]=valorYusb >> 8;   //desplazamiento de 8bits a la derecha
   EnviarByte[1]=valorYusb & 0xFF; //a & b = AND binario
}

//**** Tareas OSA ***********************************************************
#INT_TIMER0
void timer0_isr(void){  
   OS_Timer();
   set_timer0(0xE88F);    //se recarga el Timer0   
}   

void USB(void){
   for(;;){
      if(usb_enumerated()){   //True si el USB ha sido enumerado.      
         if(usb_kbhit(1)){    //(endpoint=1 EP1)= TRUE si el EP1 tiene datos 
                              //en su buffer de recepcion.
            
            //-- (endpoint,ptr,max)=Reads up to max bytes from
            //-- the specified endpoint buffer and saves it to the pointer ptr
            //-- Returns the number of bytes saved to ptr
            usb_get_packet(1,RecibirByte,20);
                                             
            //-- revisa en orden logico el contenido de 
            //-- RecibirByte[0],[1],[2],[3],[4]....
            //-- *2, OSA usa el doble de pulsos
            numPulsosX     = (RecibirByte[0]*256+RecibirByte[1])*2;  
            numPulsosY     = (RecibirByte[2]*256+RecibirByte[3])*2;
            numPulsosZ     = (RecibirByte[4]*256+RecibirByte[5])*2;
            numPulsosE     = (RecibirByte[6]*256+RecibirByte[7])*2;
            periodoPulsosX = (RecibirByte[8]*256+RecibirByte[9]);
            periodoPulsosY = (RecibirByte[10]*256+RecibirByte[11]);
            periodoPulsosZ = RecibirByte[12];
            periodoPulsosE = RecibirByte[13];
            dirMotorX      = RecibirByte[14];
            dirMotorY      = RecibirByte[15];
            dirMotorZ      = RecibirByte[16];
            dirMotorE      = RecibirByte[17];
            GlaoGlc        = RecibirByte[18];
            valorR         = RecibirByte[19];  
         }         
         //-- reviso en orden logico EnviarByte[0],[1] y envio por usb               
         ADC();    //esta funcion contiene los valores de EnviarByte[0;1]
         if( (numPulsosX || numPulsosY || numPulsosZ || numPulsosE) != 0) {EnviarByte[2]=1; output_high(PIN_B6);}
         else {EnviarByte[2]=0; output_low(PIN_B6);}
         
         //-- (endpoint,data,len,tgl)=Places the packet of data
         //-- into the specified endpoint buffer.
         //-- Returns TRUE if success, FALSE if the buffer 
         //-- is still full with the last packet.
         usb_put_packet(1,EnviarByte,3,USB_DTS_TOGGLE);                                                                     
      }      
      OS_Delay(10); 
      OS_Yield();
   }
}

void Gla(void){
   for(;;){      
      OS_Wait(GlaoGlc==3);
      //OS_Bsem_Wait(BS_GLAGLC_FREE); 
      output_toggle(PIN_B7);
      
      //-- 1023/255=4.012, necesita adaptarse al mismo rango 
      //-- porque el valorR que llega desde el GUI tiene max=255
      r=(float)(valorR*4.012);   
      control=(unsigned int16)r;
      set_pwm1_duty(control);
      
      OS_Delay(1000);
      //OS_Bsem_Set(BS_GLAGLC_FREE); 
      OS_Yield();            
   }
}

void Glc_PIDdiscreto(void){
   for(;;){  
      OS_Wait(GlaoGlc==2); //2
      //OS_Bsem_Wait(BS_GLAGLC_FREE); 
      output_toggle(PIN_B7);
    
      valorY=read_adc(ADC_READ_ONLY);
      //-- debido al ADC 10bits, la conversion se realiza con 10bits, 
      //-- entonces valorY tiene rango de 0 a 1023
      y=(float)valorY;
      //-- 1023/255=4.012, necesita adaptarse al mismo rango 
      //-- porque el valorR que llega desde el GUI tiene max=255
      r=(float)(valorR*4.012);
      //----------------------------------------------------------------------
      //-- Calculo PID por metodo tustin para termino integral 
      //-- y metodo de diferencias hacia atras para termino derivativo
      //-- Sea e(kT)=e; e(kT-T)=e1
      e=r-y;               
      //Sea p(kT)=p; i(kT)=i; i(kT-T)=i1; d(kT)=d
      p=Kpz*e;        
      //i=i1+Kiz*e;    //diferencia hacia atras
      i=i1+Kiz*(e+e1);  //tustin
      d=Kdz*(e-e1);  //diferencia hacia atras
      
      //Sea u(kT)=u
      u=p+i+d;   
       
      //-- Anti-windup solo al termino integral para evitar que se infle
      //-- y se haga muy grande si la accion de control se satura, por tanto
      //-- es necesario impedir que cambie i
      //if((u>max) | (u<min)) {i=i-Ki*T*e;}      //diferencia hacia atras
      //if((u>max) | (u<min)) i=i-Kiz*(e+e1);     //tustin
      if(u>max)u=max;
      if(u<min)u=min;
                    
      //-- realizar la conversion final
      control=(unsigned int16)u;
      set_pwm1_duty(control);           
      e1=e;
      i1=i;
      
      OS_Delay(5000);
      //OS_Bsem_Set(BS_GLAGLC_FREE);
      OS_Yield();
   }
}



void MotorX(void){
   for(;;){
      OS_Wait(numPulsosX>0);
      if(dirMotorX==64)    output_high(dirX);
      if(dirMotorX==128)   output_low(dirX);
      if(--numPulsosX!=0)  output_toggle(clockX);  
      else  output_low(clockX);     
      OS_Delay(periodoPulsosX);
      OS_Yield();
   }
}

void MotorY(void){
   for(;;){
      OS_Wait(numPulsosY>0);
      if(dirMotorY==32)    output_high(dirY);
      if(dirMotorY==16)    output_low(dirY);
      if(--numPulsosY!=0)  output_toggle(clockY);  
      else  output_low(clockY);
      OS_Delay(periodoPulsosY);
      OS_Yield();
   }
}

void MotorZ(void){
   for(;;){
      OS_Wait(numPulsosZ>0);
      if(dirMotorZ==8)     output_high(dirZ);
      if(dirMotorZ==4)     output_low(dirZ);
      if(--numPulsosZ!=0)  output_toggle(clockZ);  
      else  output_low(clockZ);     
      OS_Delay(periodoPulsosZ);
      OS_Yield();
   }
}

void MotorE(void){
   for(;;){
      OS_Wait(numPulsosE>0);
      if(dirMotorE==1)     output_high(dirE);
      if(dirMotorE==2)     output_low(dirE);
      if(--numPulsosE!=0)  output_toggle(clockE);  
      else  output_low(clockE);     
      OS_Delay(periodoPulsosE);
      OS_Yield();
   }
}


