#!python

import sys
from argparse import *

def main():
  parser = ArgumentParser(description='Turn landscape and veg proximity maps into a trained model')
  parser.add_argument('value', type=float)
  args = parser.parse_args()
  print(args.value)
  return 0

if __name__ == '__main__':
  sys.exit(main())
