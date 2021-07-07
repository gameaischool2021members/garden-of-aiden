#!python

import sys
import time
from typing import *
import numpy
from argparse import *
import matplotlib.pyplot as plt

class TrainingInstance():
  def __init__(self, data : numpy.ndarray):
    self.data = data
    pass

def main():
  parser = ArgumentParser(description='Turn landscape and veg proximity maps into a trained model')
  parser.add_argument('value', type=float)
  args = parser.parse_args()

  training_data = collect_training_data()
  trained_model = train_on_data(training_data)

  print(trained_model)

  return 0

def collect_training_data() -> List[TrainingInstance]:
  training_data = []

  for line in sys.stdin:
    if 'finish'==line.rstrip():
      break

    if 'begin_training_instance' == line.rstrip():
      serialized_training_instance = []
      for line in (line.rstrip() for line in sys.stdin):
        if 'end_training_instance'==line:
          break
        serialized_training_instance += [line]

      training_instance = parse_training_data(serialized_training_instance)

      plt.imshow(training_instance.data, cmap='hot')
      # plt.imshow(numpy.zeros([100,100]), cmap='hot')
      plt.show()

      training_data += [training_instance]

  return training_data

plants_prefix = 'plants'

def parse_plants_data(input_stream) -> numpy.ndarray:
  row_list = []
  for line in input_stream.stdin:
    if line.rstrip() == 'end':
      break
    row_list += [numpy.fromstring(line)]
  return row_list

def parse_training_data(training_data_serialized : List[str]) -> TrainingInstance:
  # if not training_data_serialized.starts_with(plants_prefix):
  #   throw 'missing prefix'

  #plants_data_serialized = training_data_serialized[len(plants_prefix):]
  assert(len(training_data_serialized) > 0)
  data = numpy.array([[float(string_element) for string_element in data_line.split(' ')] for data_line in training_data_serialized])
  return TrainingInstance(data)

def train_on_data(training_data : List[TrainingInstance]):

  # debug value to satisfy debug requirements from Unity
  # replace with serialized model when finished
  return 100

if __name__ == '__main__':
  sys.exit(main())
