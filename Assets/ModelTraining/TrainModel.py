#!python

import sys
import time
from typing import *
import numpy
from argparse import *
import matplotlib.pyplot as plt

class TrainingInstance():
  def __init__(self, plant_data : numpy.ndarray, height_data : numpy.ndarray):
    self.plant_data = plant_data
    self.height_data = height_data
    pass

def main():
  parser = ArgumentParser(description='Turn landscape and veg proximity maps into a trained model')
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

      training_data += [training_instance]

  DEBUG_plot_first_instance(training_data)

  return training_data

def DEBUG_plot_first_instance(training_data):
  training_instance = training_data[0]
  plt.imshow(training_instance.plant_data, cmap='hot')
  plt.show()
  plt.imshow(training_instance.height_data, cmap='hot')
  plt.show()

plants_prefix = 'plants'

def parse_plants_data(input_stream) -> numpy.ndarray:
  row_list = []
  for line in input_stream.stdin:
    if line.rstrip() == 'end':
      break
    row_list += [numpy.fromstring(line)]
  return row_list

def parse_training_data(training_data_serialized : List[str]) -> TrainingInstance:
  plant_data = None
  height_data = None

  line_index = 0
  while line_index < len(training_data_serialized):
    this_line = training_data_serialized[line_index].rstrip()
    line_index += 1

    if this_line == 'plants':
      plant_data = parse_serialized_numpy_array(training_data_serialized[line_index : line_index + 256])
      line_index += 256

    if this_line == 'heights':
      height_data = parse_serialized_numpy_array(training_data_serialized[line_index : line_index + 256])
      line_index += 256

  return TrainingInstance(plant_data, height_data)

def parse_serialized_numpy_array(serialized_numpy_data : List[str]) -> numpy.ndarray:
  return numpy.array([[float(string_element) for string_element in data_line.split(' ')] for data_line in serialized_numpy_data])

def train_on_data(training_data : List[TrainingInstance]):

  # debug value to satisfy debug requirements from Unity
  # replace with serialized model when finished
  return 100

if __name__ == '__main__':
  sys.exit(main())
