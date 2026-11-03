"""Original quiet service-floor utility texture, no baked lighting or threat markings."""
from PIL import Image,ImageDraw
from pathlib import Path
import math
root=Path(__file__).resolve().parents[2]
image=Image.new('RGB',(512,512));pixels=image.load()
for y in range(512):
    for x in range(512):
        value=222+round(1.5*math.sin(x*.07)*math.sin(y*.031))
        pixels[x,y]=(value,value,value)
draw=ImageDraw.Draw(image)
draw.rectangle((3,3,508,508),outline=(201,201,201),width=3)
draw.line((256,4,256,508),fill=(210,210,210),width=2)
for x in (14,498):
    for y in (14,498):draw.rectangle((x-2,y-2,x+2,y+2),fill=(194,194,194))
image.save(root/'Assets/_Project/Art/RunExp05/Environment/ArchiveFloor.png')
