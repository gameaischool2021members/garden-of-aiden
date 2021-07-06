#!python

import sys
import typing
from argparse import *

class TrainingInstance():
  def __init__(self):
    pass

def main():
  parser = ArgumentParser(description='Turn landscape and veg proximity maps into a trained model')
  parser.add_argument('value', type=float)
  args = parser.parse_args()

  training_data = collect_training_data()
  trained_model = train_on_data(training_data)

  print(trained_model)

  return 0

def collect_training_data() -> typing.List[TrainingInstance]:
  training_data = []

  for line in sys.stdin:
    if 'finish'==line.rstrip():
      break

    training_data += [parse_training_data(line)]

  return training_data

def parse_training_data(training_data_serialized : str) -> TrainingInstance:
  return TrainingInstance()

def train_on_data(training_data : typing.List[TrainingInstance]):

  # debug value to satisfy debug requirements from Unity
  # replace with serialized model when finished
  return 100

if __name__ == '__main__':
  sys.exit(main())
