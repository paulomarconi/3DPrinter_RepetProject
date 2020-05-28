%Gp de 1er orden
Kp=0.95129; Tp1=0.10921;
Gp1=tf([Kp],[Tp1 1])
b=Kp/Tp1
a=1/Tp1
%Gp de 2do orden
Kp=0.93548; Tp1=0.051052; Tp2=0.05105;
Gp2=tf([Kp],[(Tp1*Tp2) (Tp1+Tp2) 1])
step(Gp1,Gp2)