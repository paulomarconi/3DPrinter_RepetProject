s=tf('s');
Gp=1.4958/(1+429.15*s);
Kd=4.372; Kp=4.387; Ki=0.01457; 
Gc=tf([Kd Kp Ki],[1 0]); % Gc=0.014574*(1+s)*(1+300*s)/s
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