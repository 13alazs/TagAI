# TagAI
This repo contains the necessary files to run my thesis work. It runs in Unity 2020.3.8.
This is a machine learning models that uses genetic algorithm and neural networks to teach AI a simple 2D version of tag.
The catchers are red, the runners are white and there are green circles witch are safe places. Every player have 5 sensors that represents their vision.
For more information read the thesis.pdf, although it's in hungarian. Here are some interesting behaviours which I observed:

Runners following each other in a line while also following a catcher since they can't see what's behind them.
![Follow](/Examples/field5_follow.png "Follow")

Catchers creating a wall between the runners and the safe zones.
![Line](/Examples/field5_line.png "Line")

Runners running around in a circle so they can detect catchers from every direction.
![Circle](/Examples/field5_spiral.png "Circle")
