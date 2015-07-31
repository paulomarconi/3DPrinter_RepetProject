s=tf('s');
Gp=0.92935/(1+590.8*s);
Kd=3.864; Kp=3.874; Ki=0.009661; 
Gc=tf([Kd Kp Ki],[1 0]); % Gc=0.0096605*(1+s)*(1+400*s)/s
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