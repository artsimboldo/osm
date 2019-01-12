import sys
from sys import argv
import requests
#from lxml import etree as et
#import simplejson as json
import time

ENDPOINT = 'http://api.openstreetmap.org/api/0.6/map?'

def dump(left, bottom, right, top):
	url = ENDPOINT + 'bbox=' + str(left) + ',' + str(bottom) + ',' + str(right) + ',' + str(top)
	print "Reading data (GET " + url + ")..."
	r = requests.get(url, stream=True)
	if r.status_code == requests.codes.ok:
		return r.text
	else:
		print(r.status_code)
		return None;

if __name__ == '__main__':
	reload(sys)
	# python default encoding is ascii, need unicode everywhere
	sys.setdefaultencoding('utf-8')	
	left, bottom, right, top, filename = 2.3056, 48.8511, 2.3085, 48.8533, "ecole-militaire2.xml"
	result = dump(left, bottom, right, top);
	if result is not None:
		print "Writing " + filename + "..."
		fo = open(filename, "wb")
		fo.write(result);
		fo.close()
