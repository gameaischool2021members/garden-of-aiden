from time import sleep
import numpy as np
import pickle
from typing import *
from numpy import zeros
from numpy import ones
from numpy.random import randint
from tensorflow.keras.optimizers import Adam
from tensorflow.keras.initializers import RandomNormal
from tensorflow.keras.models import Model
from tensorflow.keras.models import load_model
from tensorflow.keras.layers import Input
from tensorflow.keras.layers import Conv2D
from tensorflow.keras.layers import Conv2DTranspose
from tensorflow.keras.layers import LeakyReLU
from tensorflow.keras.layers import Activation
from tensorflow.keras.layers import Concatenate
from tensorflow.keras.layers import Dropout
from tensorflow.keras.layers import BatchNormalization
from tensorflow.keras.layers import LeakyReLU
from matplotlib import pyplot

from tensorflow.keras.preprocessing.image import ImageDataGenerator
from tensorflow.keras.datasets.fashion_mnist import load_data
from numpy import expand_dims

from cDCGAN import Pipeline

# define the discriminator model
def define_discriminator(image_shape):
	# weight initialization
	init = RandomNormal(stddev=0.02)
	# source image input
	in_src_image = Input(shape=image_shape)
	# target image input
	in_target_image = Input(shape=image_shape)
	# concatenate images channel-wise
	merged = Concatenate()([in_src_image, in_target_image])
	# C64
	d = Conv2D(16, (8, 8), strides=(2,2), padding='same', kernel_initializer=init)(merged)
	d = LeakyReLU(alpha=0.2)(d)
	# C128
	d = Conv2D(32, (8, 8), strides=(2,2), padding='same', kernel_initializer=init)(d)
	d = BatchNormalization()(d)
	d = LeakyReLU(alpha=0.2)(d)
	# C256
	d = Conv2D(64, (8, 8), strides=(2,2), padding='same', kernel_initializer=init)(d)
	d = BatchNormalization()(d)
	d = LeakyReLU(alpha=0.2)(d)
	# C512
	d = Conv2D(128, (8, 8), strides=(2,2), padding='same', kernel_initializer=init)(d)
	d = BatchNormalization()(d)
	d = LeakyReLU(alpha=0.2)(d)
	# second last output layer
	d = Conv2D(128, (8, 8), padding='same', kernel_initializer=init)(d)
	d = BatchNormalization()(d)
	d = LeakyReLU(alpha=0.2)(d)
	# output
	d = Conv2D(1, (8, 8), padding='same', kernel_initializer=init)(d)
	out_layer = Activation('sigmoid')(d)
	# define model
	model = Model([in_src_image, in_target_image], out_layer)
	# compile model
	opt = Adam(lr=0.0002, beta_1=0.5)
	model.compile(loss='binary_crossentropy', optimizer=opt, loss_weights=[0.5])
	return model

# define an encoder block
def define_encoder_block(layer_in, n_filters, batchnorm=True):
	# weight initialization
	init = RandomNormal(stddev=0.02)
	# add downsampling layer
	g = Conv2D(n_filters, (8, 8), strides=(2, 2), padding='same', kernel_initializer=init)(layer_in)
	# conditionally add batch normalization
	if batchnorm:
		g = BatchNormalization()(g, training=True)
	# leaky relu activation
	g = LeakyReLU(alpha=0.2)(g)
	return g

# define a decoder block
def decoder_block(layer_in, skip_in, n_filters, dropout=True):
	# weight initialization
	init = RandomNormal(stddev=0.02)
	# add upsampling layer
	g = Conv2DTranspose(n_filters, (8, 8), strides=(2, 2), padding='same', kernel_initializer=init)(layer_in)
	# add batch normalization
	g = BatchNormalization()(g, training=True)
	# conditionally add dropout
	if dropout:
		g = Dropout(0.5)(g, training=True)
	# merge with skip connection
	g = Concatenate()([g, skip_in])
	# relu activation
	g = Activation('relu')(g)
	return g

# define the standalone generator model
def define_generator(image_shape=(256,256,3)):
	# weight initialization
	init = RandomNormal(stddev=0.02)
	# image input
	in_image = Input(shape=image_shape)
	# encoder model
	e1 = define_encoder_block(in_image, 16, batchnorm=False)
	e2 = define_encoder_block(e1, 32)
	e3 = define_encoder_block(e2, 64)
	e4 = define_encoder_block(e3, 128)
	e5 = define_encoder_block(e4, 128)
	#e6 = define_encoder_block(e5, 128)
	#e7 = define_encoder_block(e6, 128)
	# bottleneck, no batch norm and relu
	b = Conv2D(128, (8, 8), strides=(2,2), padding='same', kernel_initializer=init)(e5)
	b = Activation('relu')(b)
	# decoder model
	#d1 = decoder_block(b, e7, 128)
	#d2 = decoder_block(b, e6, 128)
	d3 = decoder_block(b, e5, 128)
	d4 = decoder_block(d3, e4, 128, dropout=False)
	d5 = decoder_block(d4, e3, 64, dropout=False)
	d6 = decoder_block(d5, e2, 32, dropout=False)
	d7 = decoder_block(d6, e1, 16, dropout=False)
	# output
	g = Conv2DTranspose(3, (8, 8), strides=(2,2), padding='same', kernel_initializer=init)(d7)
	out_image = Activation('tanh')(g)
	# define model
	model = Model(in_image, out_image)
	return model

# define the combined generator and discriminator model, for updating the generator
def define_gan(g_model, d_model, image_shape):
	# make weights in the discriminator not trainable
	for layer in d_model.layers:
		if not isinstance(layer, BatchNormalization):
			layer.trainable = False
	# define the source image
	in_src = Input(shape=image_shape)
	# connect the source image to the generator input
	gen_out = g_model(in_src)
	# connect the source input and generator output to the discriminator input
	dis_out = d_model([in_src, gen_out])
	# src image as input, generated image and classification output
	model = Model(in_src, [dis_out, gen_out])
	# compile model
	opt = Adam(lr=0.0002, beta_1=0.5)
	model.compile(loss=['binary_crossentropy', 'mae'], optimizer=opt, loss_weights=[1,100])
	return model


def load_random_samples(shape=(1000, 64, 64, 3)):
	# generate random dataset
	trainy = np.random.default_rng().uniform(size=shape)
	trainX = np.random.default_rng().uniform(size=shape)
	return [trainX, trainy]

# select a batch of random samples, returns images and target
def generate_real_samples(dataset, n_samples, patch_shape):
	# unpack dataset
	trainA, trainB = dataset
	# choose random instances
	ix = randint(0, trainA.shape[0], n_samples)
	# retrieve selected images
	X1, X2 = trainA[ix], trainB[ix]
	# generate 'real' class labels (1)
	y = ones((n_samples, patch_shape, patch_shape, 1))
	return [X1, X2], y


# generate a batch of images, returns images and targets
def generate_fake_samples(g_model, samples, patch_shape):
	# generate fake instance
	X = g_model.predict(samples)
	# create 'fake' class labels (0)
	y = zeros((len(X), patch_shape, patch_shape, 1))
	return X, y


# generate samples and save as a plot and save the model
def summarize_performance(step, g_model, dataset, n_samples=3):
	# select a sample of input images
	[X_realA, X_realB], _ = generate_real_samples(dataset, n_samples, 1)
	# generate a batch of fake samples
	X_fakeB, _ = generate_fake_samples(g_model, X_realA, 1)
	# scale all pixels from [-1,1] to [0,1]
	X_realA = (X_realA + 1) / 2.0
	X_realB = (X_realB + 1) / 2.0
	X_fakeB = (X_fakeB + 1) / 2.0
	# plot real source images
	for i in range(n_samples):
		pyplot.subplot(3, n_samples, 1 + i)
		pyplot.axis('off')
		pyplot.imshow(X_realA[i])
	# plot generated target image
	for i in range(n_samples):
		pyplot.subplot(3, n_samples, 1 + n_samples + i)
		pyplot.axis('off')
		pyplot.imshow(X_fakeB[i])
	# plot real target image
	for i in range(n_samples):
		pyplot.subplot(3, n_samples, 1 + n_samples*2 + i)
		pyplot.axis('off')
		pyplot.imshow(X_realB[i])
	# save plot to file
	filename1 = 'plot_%06d.png' % (step+1)
	pyplot.savefig(filename1)
	pyplot.close()
	# save the generator model
	filename2 = 'model_%06d.h5' % (step+1)
	g_model.save(filename2)
	print('>Saved: %s and %s' % (filename1, filename2))


# train models
def train(d_model, g_model, gan_model, dataset, n_epochs=1, n_batch=32, augmented=False):
	# determine the output square shape of the discriminator
	n_patch = d_model.output_shape[1]

	if (augmented):
		dataset = next(augment_data(dataset, batch_size=dataset[0].shape[0] * 10))

	# unpack dataset
	trainA, trainB = dataset

	# calculate the number of batches per training epoch
	bat_per_epo = int(len(trainA) / n_batch)
	# calculate the number of training iterations
	n_steps = bat_per_epo * n_epochs
	# manually enumerate epochs
	for i in range(n_steps):
		# select a batch of real samples
		[X_realA, X_realB], y_real = generate_real_samples(dataset, n_batch, n_patch)
		# generate a batch of fake samples
		X_fakeB, y_fake = generate_fake_samples(g_model, X_realA, n_patch)
		# update discriminator for real samples
		d_loss1 = d_model.train_on_batch([X_realA, X_realB], y_real)
		# update discriminator for generated samples
		d_loss2 = d_model.train_on_batch([X_realA, X_fakeB], y_fake)
		# update the generator
		g_loss, _, _ = gan_model.train_on_batch(X_realA, [y_real, X_realB])
		# summarize performance
		print('>%d, d1 (real sample loss)[%.3f] d2 (generated sample loss)[%.3f] g[%.3f]' % (i+1, d_loss1, d_loss2, g_loss))
		# summarize model performance
		if (i+1) % (bat_per_epo * 10) == 0:
			summarize_performance(i, g_model, dataset)

# create iterators for both sets of images with rotation, zoom, width and height shift applied
def augment_data(dataset, batch_size=1):
	X, y = dataset
	seed = 1
	# generator params
	data_gen_args = dict(
		rotation_range=90,
        zoom_range=0.1,
        width_shift_range=0.1,
        height_shift_range=0.1,
        horizontal_flip=True,
        vertical_flip=True
		)
	# create and fit both generators with the same seed and args
	datagen_x = ImageDataGenerator(**data_gen_args)
	datagen_y = ImageDataGenerator(**data_gen_args)
	datagen_x.fit(X, seed=seed)
	datagen_y.fit(y, seed=seed)

	return zip(datagen_x.flow(X, seed=seed, batch_size=batch_size), datagen_y.flow(y, seed=seed, batch_size=batch_size))


def verify_augmentation():
	# load some data
	(trainX, _), (_, _) = load_data()
	(trainZ, _), (_, _) = load_data()
	# augment data
	augmented_data = augment_data([expand_dims(trainX, axis=-1), expand_dims(trainZ, axis=-1)], batch_size=trainX.shape[0])
	# verify data is one-to-one
	for _ in range(10):
		a, b = next(augmented_data)
		print((a == b).all())
		print(a.shape)
	
	a, b = next(augmented_data)
	assert((a == b).all())


# dataset type: [np.Array, np.Array]
# each array must have the same shape
# dataset[0] should be the input data to the generator
# dataset[1] should be the example of the corresponding vegetation map
def load_data_and_train(dataset=None, epochs=100, generator_model=None):
	# load image data
	if dataset is None:
		dataset = load_random_samples()
		
	print('Loaded', dataset[0].shape, dataset[1].shape)
	# define input shape based on the loaded dataset
	image_shape = dataset[0].shape[1:]
	print('Image shape: ', image_shape)
	# define the models
	d_model = define_discriminator(image_shape)

	g_model = define_generator(image_shape)
	if (generator_model is not None):
		g_model = generator_model

	# define the composite model
	gan_model = define_gan(g_model, d_model, image_shape)
	# train model
	train(d_model, g_model, gan_model, dataset, n_epochs=epochs, augmented=True)
	return g_model
	

class ModelRunner:
	def __init__(self, path_to_model) -> None:
		self.model = load_model(path_to_model)

	def infer(self, input: List[Pipeline.TrainingInstance]) -> np.ndarray:
		input = Pipeline.reshape_data_for_inference(input)
		return self.model.predict(input)

	def retrain(self, dataset, epochs):
		dataset = Pipeline.reshape_data_for_training(dataset)
		self.model = load_data_and_train(dataset, epochs, generator_model=self.model)
		
	def listen(self):
		print("listening")
		while(True):
			sleep(0.1)
			dataset = Pipeline.collect_inference_data()
			if (dataset != []):
				output = self.infer(dataset)
				#serialized_out = pickle.dumps(output, protocol=0)
				print('TreeProxMap')
				print_serialized_arr(output, 0)
				print_serialized_arr(output, 1)
				# serialize & sent output to stdout


def print_serialized_arr(numpy_arr, channel):
	# numpy_arr.shape is (1, 256, 256, 3)
	first_element = numpy_arr[0]
	# select last channel out of the array and remove its channel dimension: (256, 256, 3) -> (256, 256)
	first_element = first_element[:, :, channel]
	for row in first_element:
		print(' '.join(str(val) for val in row))
	print('', flush=True)
	pass

# for rapid testing with different data shapes
import sys
if __name__ == "__main__":
	sys.exit(load_data_and_train(None))
