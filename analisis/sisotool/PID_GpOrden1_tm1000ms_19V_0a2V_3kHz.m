s=tf('s');
Gp=1.1835/(1+490.58*s);
Kd=1.289; Kp=1.292; Ki=0.00379; 
Gc=tf([Kd Kp Ki],[1 0]); % Gc=0.0037898*(1+s)*(1+340*s)/s
T=10;    % tr/6 < T < tr/20
Kpz=Kp; Kiz=Ki*T/2; Kdz=Kd/T; 
Gpz=c2d(Gp,T,'zoh');
Gcz=tf([(Kpz+Kiz+Kdz) (-Kpz+Kiz-2*Kdz) Kdz],[1 -1 0],T);
Glaz=Gpz*Gcz; Gla=Gp*Gc;
Glcz=feedback(Glaz,1); Glc=feedback(Gla,1);
step(Gpz,Glcz,Glc)
pause
margin(Glcz)
pause
bode(Glcz,Glc)