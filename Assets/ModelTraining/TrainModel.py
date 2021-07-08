#!python

import sys
from typing import *
from argparse import *
from cDCGAN import Model

#! important
# make sure to run `$ pip install -e .` in ModelTraining directory while inside python env
from cDCGAN import Model 
from cDCGAN import Pipeline
from cDCGAN.Pipeline import TrainingInstance



def main():
  parser = ArgumentParser(description='Turn landscape and veg proximity maps into a trained model')
  args = parser.parse_args()

  training_data = Pipeline.collect_training_data()
  trained_model = train_on_data(training_data)

  print(trained_model)

  return 0


plants_prefix = 'plants'


def train_on_data(training_data : List[TrainingInstance]):
  dataset = Pipeline.reshape_data_for_training(training_data)

  generator_model = Model.load_data_and_train(dataset, epochs=100)
  generator_model.save('.\saved_model.keras')

  # debug value to satisfy debug requirements from Unity
  # replace with serialized model when finished
  return 100


if __name__ == '__main__':
  sys.exit(main())
