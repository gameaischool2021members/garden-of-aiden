import numpy
from typing import *
import sys
import matplotlib.pyplot as plt

class TrainingInstance():
  def __init__(self, plant_data : numpy.ndarray, height_data : numpy.ndarray):
    self.plant_data = plant_data
    self.height_data = height_data
    pass

def collect_inference_data() -> List[TrainingInstance]:
  inference_data = []

  print("collecting inference data")
  for line in sys.stdin:
    if 'finish'==line.rstrip():
      break

    if 'begin_inference_instance' == line.rstrip():
      print("found start of inference instance")
      serialized_inference_instance = []
      for line in (line.rstrip() for line in sys.stdin):
        if 'end_inference_instance'==line:
          break
        serialized_inference_instance += [line]

      inference_instace = parse_training_data(serialized_inference_instance)

      inference_data += [inference_instace]
      print("collected inference instance")

  print("finished collecting inference data")
  return inference_data
    

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
  dataset = reshape_data_for_training(training_data)
  training_instance = training_data[0]
  plt.title(dataset[1].shape)
  plt.imshow(training_instance.plant_data, cmap='hot')
  plt.show()
  plt.title(dataset[0].shape)
  plt.imshow(training_instance.height_data, cmap='hot')
  plt.show()

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


def reshape_data_for_inference(training_data: List[TrainingInstance]) -> numpy.ndarray:
      # split training instances
  input_data_height = [] # Height input channel
  for i in range(len(training_data)):
    input_data_height.append(training_data[i].height_data)

  # convert to numpy arrays
  input_data_height = numpy.expand_dims(numpy.stack(input_data_height, axis=0), axis= -1)
  input_data_height_shape = input_data_height.shape
  input_data = numpy.broadcast_to(input_data_height, (input_data_height_shape[0], input_data_height_shape[1], input_data_height_shape[2], 3)).copy()

  return input_data

def reshape_data_for_training(training_data: List[TrainingInstance]) -> List[numpy.ndarray]:
  # split training instances
  input_data_height = [] # Height input channel
  example_data_plant = [] # Plant real output channel
  for i in range(len(training_data)):
    input_data_height.append(training_data[i].height_data)
    example_data_plant.append(training_data[i].plant_data)

  # convert to numpy arrays
  input_data_height = numpy.expand_dims(numpy.stack(input_data_height, axis=0), axis= -1)
  input_data_height_shape = input_data_height.shape
  input_data = numpy.broadcast_to(input_data_height, (input_data_height_shape[0], input_data_height_shape[1], input_data_height_shape[2], 3)).copy()

  example_data_plant = numpy.expand_dims(numpy.stack(example_data_plant, axis=0), axis= -1)
  example_data_plant_shape = example_data_plant.shape
  example_data = numpy.broadcast_to(example_data_plant, (example_data_plant_shape[0], example_data_plant_shape[1], example_data_plant_shape[2], 3)).copy()
  return [input_data, example_data]
