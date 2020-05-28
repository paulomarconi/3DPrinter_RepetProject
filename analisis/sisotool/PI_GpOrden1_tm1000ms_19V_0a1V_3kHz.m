s=tf('s');
Gp=1.1835/(1+490.58*s);
Kp=2.58; Ki=0.007589; 
Gc=tf([Kp Ki],[1 0]); %  Gc=0.0075894*(1+340*s)/s
Gla=Gp*Gc;
Glc=feedback(Gla,1);
step(Gp,Glc)

