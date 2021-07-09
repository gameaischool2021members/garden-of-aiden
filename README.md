# Garden of AIden
## Description

This tool learns at design time how to place plants based on the environment. When playing the game, the player can shift mountains and hills to change the environment, and the vegetation will update to match its new situation, based on what the agent learnt earlier.

## How to install

 1. Clone the project.
 2. Open the project in Unity. The version used to develop the app was 2020.3.13f1.
 3. Go to the sandbox scene.
 4. Hit play, and start editing the terrain by simply clicking around. The updates to the vegetation should be visible instantaneously.

## System

### AI Core
We are using a customized GAN to learn the desired pattern and rules of the artist initially. After the training is finished, we can use the model to predict the vegetation of the desired terrain which can be modified during the runtime.
The inputs of the system are the terrain height-map and the current plants textures.
![System description]("./Readme Pictures/System Description.png")
#### An example
![A training example]("./Readme Pictures/AI Example.png")
The first row of this picture show the height-maps.
And the last row is the final outputs of the system with two kinds of plants.

### Unity Interface
The user can edit the terrain at runtime, and the changes to the vegetation will be applied.

### Scanning and Placing the vegetation
Currently there are only 2 types vegetation implemented, but the model is expandable and can be used for as many types of plants as needed.
The module "scanner" can be used for initial phase of training the model, "to scan" the terrain and create the inputs for the GAN.
The module "placer" then interprets the GAN outputs to update the terrain.
The communication happens with the means of textures, where each type of plant has its own texture channel in the training model, and the final results will be in the form of multi-channel as well. Then each texture channel is passed to the placer, where based on the brightest points in the map, the plants' positions are determined and they will be instantiated.

## Made by

- Anthony Diggle
- Sam Fay-Hunt
- Quirin Maier
- Gema Parreno
- Cristiana Pacheco
- Kian Razavi Satvati
