"""Prepare licensed source effects. Run from project root; no Unity serialization edits.

Requires numpy and soundfile. Original downloads/extractions live in Temp/sfx-work;
the retained archives under Assets/Audio/SFX/Sources can reconstruct that workspace.
Each prepared asset records source layers, pitch/time edits and measured peaks.
"""
from pathlib import Path
import sys, json, shutil, hashlib
sys.path.insert(0, 'Temp/sfx-work/pythonpackages')
import numpy as np
import soundfile as sf

RATE = 44100
root = Path('Temp/sfx-work')
destination = Path('Assets/Audio/SFX')
records, recipes = [], []

def read(relative, pitch=1, duration=None):
    x, rate = sf.read(root / relative, always_2d=True)
    x = x.mean(axis=1)
    activity = np.flatnonzero(np.abs(x) > max(.0003, np.max(np.abs(x)) * .003))
    if len(activity): x = x[max(0, activity[0]-int(rate*.006)):min(len(x),activity[-1]+int(rate*.04))]
    count = max(2, round(len(x) * RATE / rate / pitch))
    x = np.interp(np.arange(count)*rate*pitch/RATE, np.arange(len(x)), x)
    if duration: x = x[:round(duration*RATE)]
    return x

def prepare(cue, folder, layers, volume=.5, priority=100, interval=.09, concurrent=3, variation=''):
    end = max(round(offset*RATE)+len(read(src,pitch,cut)) for src,pitch,gain,offset,cut in layers)
    x = np.zeros(end)
    for src,pitch,gain,offset,cut in layers:
        part=read(src,pitch,cut); start=round(offset*RATE); x[start:start+len(part)] += part*gain
    fade=min(round(RATE*.004),len(x)//3)
    x[:fade]*=np.linspace(0,1,fade); x[-fade:]*=np.linspace(1,0,fade)
    x *= 10**(-6/20)/max(np.max(np.abs(x)),1e-10)
    path=destination/folder/(cue+variation+'.wav');path.parent.mkdir(parents=True,exist_ok=True)
    sf.write(path,x,RATE,subtype='PCM_16')
    inspect(path,layers)
    recipes.append(dict(cue=cue,path=str(path).replace('\\','/'),volume=volume,priority=priority,interval=interval,concurrent=concurrent))

def inspect(path,layers):
    x,rate=sf.read(path,always_2d=True)
    record=dict(path=str(path).replace('\\','/'),duration=round(len(x)/rate,4),sampleRate=rate,channels=x.shape[1],peakDbFS=round(20*np.log10(max(np.max(np.abs(x)),1e-10)),3),rmsDbFS=round(20*np.log10(max(np.sqrt(np.mean(x*x)),1e-10)),3),clippedSamples=int(np.count_nonzero(np.abs(x)>=.999)),firstSampleMax=float(np.max(np.abs(x[0]))),lastSampleMax=float(np.max(np.abs(x[-1]))),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),layers=layers)
    records.append(record)

impact=lambda name:'impact-sounds/Audio/'+name+'.ogg'
rpg=lambda name:'rpg-audio/Audio/'+name+'.ogg'
ui=lambda name:'interface-sounds/Audio/'+name+'.ogg'
swish=lambda n:'swishes-sound-pack/swishes/swish-'+str(n)+'.wav'
leaf=lambda n:f'leaves/LeavesTest001/LeavesTest001 {n:03}.flac'
splat=lambda n:f'independent_nu_ljudbank-wet_squish_slurp_impacts/impsplat/impactsplat{n:02}.mp3.flac'
layer=lambda source,pitch=1,gain=1,offset=0,cut=None:(source,pitch,gain,offset,cut)
magic='magical_6_0.ogg'; fire='105016__julien-matthey__jm-fx-fireball-01.wav'

for i in range(3):
    prepare('Footstep','Player',[layer(impact(f'footstep_grass_{i:03}'))],.24,180,.17,3,f'_{i+1}')
    prepare('Land','Player',[layer(impact(f'impactSoft_medium_{i:03}')),layer(impact(f'footstep_grass_{i:03}'),1,.45)],.35,160,.12,3,f'_{i+1}')
prepare('Jump','Player',[layer(swish(2),.85),layer(rpg('cloth1'),1,.25)],.38,140)
prepare('DoubleJump','Player',[layer(swish(4),.75),layer(swish(3),.9,.8,.11),layer(rpg('cloth2'),.85,.25,.07)],.4,130)
prepare('HardLand','Player',[layer(impact('impactSoft_heavy_000'),.9),layer(impact('impactMining_001'),1,.2)],.5,140)
for i in range(2):
    prepare('TailSweep','Player',[layer(swish(7+i),.65),layer(rpg('cloth3'),.8,.3)],.53,100,.08,3,f'_{i+1}')
    prepare('FireLaunch','Player',[layer(fire,1+i*.06,1,0,.65)],.45,100,.12,3,f'_{i+1}')
    prepare('FireImpact','Player',[layer(fire,.9, .55,0,.6),layer(impact(f'impactSoft_heavy_{i:03}'),.8)],.5,110,.14,3,f'_{i+1}')
prepare('PlayerHurt','Player',[layer(impact('impactSoft_heavy_001'),1),layer(rpg('cloth2'),.9,.35)],.5,70,.2,1)
prepare('PlayerDeath','Player',[layer(impact('impactSoft_heavy_002'),.65),layer(swish(7),.65,.4,.12)],.55,40,.5,1)

prepare('ChomperWarn','Enemies',[layer(leaf(3),.8),layer(leaf(3),.65,.65,.2)],.52,35,.2,3)
prepare('ChomperBite','Enemies',[layer(rpg('chop')),layer(leaf(10),.9,.7)],.6,80,.15,3)
for i in range(2):
    prepare('ChomperHit','Enemies',[layer(impact(f'impactSoft_medium_{i:03}')),layer(leaf(3+i),1,.45)],.42,115,.1,3,f'_{i+1}')
prepare('ChomperDeath','Enemies',[layer(leaf(3),.65),layer(impact('impactSoft_heavy_001'),.75,.5,.1)],.58,45,.4,2)
prepare('SporaWarn','Enemies',[layer(splat(5),.85,1,0,.55),layer(splat(2),.7,.65,.2,.45)],.48,35,.2,3)
for i in range(2):
    prepare('SporaLaunch','Enemies',[layer(swish(5+i),.7),layer(splat(7+i),1,.6,0,.25)],.48,90,.2,3,f'_{i+1}')
    prepare('SporeImpact','Enemies',[layer(splat(7+i),1)],.42,120,.16,3,f'_{i+1}')
    prepare('SporaHit','Enemies',[layer(splat(1+i),1,1,0,.45),layer(impact(f'impactSoft_medium_{i:03}'),1,.3)],.4,115,.1,3,f'_{i+1}')
prepare('SporaDeath','Enemies',[layer(splat(5),.7),layer(leaf(3),.9,.3,.15)],.5,45,.4,2)
for i in range(2):
    prepare('RockStep','Enemies',[layer(impact(f'impactMining_{i:03}'),.8),layer(impact(f'impactSoft_heavy_{i:03}'),.7,.6)],.4,175,.25,2,f'_{i+1}')
    prepare('RockHit','Enemies',[layer(impact(f'impactMining_{i+2:03}'),.85),layer(impact(f'impactSoft_medium_{i:03}'),1,.5)],.45,110,.12,3,f'_{i+1}')
prepare('RockWarn','Enemies',[layer(impact('impactMining_001'),.55),layer(impact('impactMining_003'),.65,.55,.25)],.58,30,.25,3)
prepare('RockSlam','Enemies',[layer(impact('impactMining_004'),.65),layer(impact('impactSoft_heavy_003'),.65,.8)],.65,70,.25,2)
prepare('RockDeath','Enemies',[layer(impact('impactMining_002'),.65),layer(impact('impactMining_004'),.65,.8,.16),layer(leaf(3),.75,.5,.3)],.6,40,.4,2)

prepare('BossSlamWarn','Boss',[layer(impact('impactMining_001'),.5),layer(impact('impactMining_003'),.5,.9,.3),layer(leaf(3),.7,.4,.45)],.65,20,.3,1)
prepare('BossLungeWarn','Boss',[layer(leaf(3),.65),layer(rpg('creak2'),.65,.55,.18)],.62,20,.3,1)
prepare('BossSporeWarn','Boss',[layer(splat(5),.6,1,0,.7),layer(splat(2),.55,.7,.2,.55)],.65,20,.3,1)
prepare('BossSlam','Boss',[layer(impact('impactMining_004'),.5),layer(impact('impactSoft_heavy_003'),.55,1.2)],.7,40,.2,1)
prepare('BossShockwave','Boss',[layer(impact('impactMining_003'),.6),layer(swish(9),.5,.8)],.45,75,.25,2)
prepare('BossLunge','Boss',[layer(swish(9),.5),layer(rpg('chop'),.7,.6,.15),layer(leaf(3),.8,.4)],.65,40,.3,1)
prepare('BossVolley','Boss',[layer(swish(8),.6),layer(splat(7),.7,.7),layer(splat(8),.8,.5,.12)],.55,40,.3,1)
prepare('BossHit','Boss',[layer(impact('impactMining_002'),.7),layer(splat(7),.8,.35)],.48,90,.12,2)
prepare('BossPhase','Boss',[layer(impact('impactMining_003'),.55),layer('magical_4.ogg',.6,.45),layer(leaf(3),.7,.4,.25)],.65,15,.8,1)
prepare('BossDeath','Boss',[layer(impact('impactMining_004'),.55),layer(impact('impactMining_002'),.55,.7,.25),layer(splat(5),.75,.35,.2),layer(leaf(3),.65,.4,.6)],.7,10,1,1)
prepare('WhisperseedRecruit','Blooms',[layer(magic,1)],.35,130,.3,2)
prepare('DryadRecruit','Blooms',[layer(magic,.92)],.35,130,.3,2)
prepare('PetalbornRecruit','Blooms',[layer(magic,1.08)],.35,130,.3,2)
prepare('CrystalAppear','Boss',[layer('magical_3.ogg',.9)],.45,45,.5,1)
prepare('CrystalTouch','Boss',[layer(magic,1.08)],.45,40,.5,1)
prepare('MenuFocus','UI',[layer(ui('pluck_001'))],.2,140,.12,2)
prepare('MenuClick','UI',[layer(ui('click_003')),layer(ui('pluck_002'),1,.4)],.35,60,.15,2)
prepare('Pause','UI',[layer(ui('pluck_001'),.85)],.35,55,.3,1)
prepare('Resume','UI',[layer(ui('pluck_002'),1.1)],.35,55,.3,1)

# Retain archives in full, plus original standalone recordings and supplied licenses.
packs=[('KenneyRPG','kenney_rpg-audio.zip','rpg-audio/License.txt','Kenney (Kenney Vleugels)','https://kenney.nl/assets/rpg-audio','CC0'),
       ('KenneyImpacts','kenney_impact-sounds.zip','impact-sounds/License.txt','Kenney (Kenney Vleugels)','https://kenney.nl/assets/impact-sounds','CC0'),
       ('KenneyInterface','kenney_interface-sounds.zip','interface-sounds/License.txt','Kenney (Kenney Vleugels)','https://kenney.nl/assets/interface-sounds','CC0'),
       ('Swishes','swishes.zip',None,'artisticdude','https://opengameart.org/content/swishes-sound-pack','CC0'),
       ('WetSquish','independent_nu_ljudbank-wet_squish_slurp_impacts.7z',None,'Independent.nu','https://opengameart.org/content/8-wet-squish-slurp-impacts','CC0'),
       ('Leaves','leaves.7z','leaves/LeavesTest001/info.txt','Tim Mortimer','https://opengameart.org/content/leaves-cracking-and-crumbling','CC BY 3.0'),
       ('Fireball',fire,None,'Julien Matthey','https://opengameart.org/content/fireball-1','CC0'),
       ('Magic',None,None,'JaggedStone','https://opengameart.org/content/magic-spell-sfx','CC0'),
       ('Wind','wind woosh loop.ogg',None,'SketchMan3','https://opengameart.org/content/wind-whoosh-loop','CC0')]
sources=[]
for name,archive,licensefile,creator,url,license in packs:
    target=destination/'Sources'/name;target.mkdir(parents=True,exist_ok=True)
    files=[root/archive] if archive else [root/magic,root/'magical_3.ogg',root/'magical_4.ogg']
    for file in files: shutil.copy2(file,target/file.name)
    if licensefile:shutil.copy2(root/licensefile,target/Path(licensefile).name)
    licenseurl='https://creativecommons.org/publicdomain/zero/1.0/' if license=='CC0' else 'https://creativecommons.org/licenses/by/3.0/'
    license_target=Path('Docs')/target
    license_target.mkdir(parents=True,exist_ok=True)
    (license_target/'LICENSE.md').write_text(f'# {name}\n\nCreator: {creator}\n\nSource: {url}\n\nLicense: [{license}]({licenseurl})\n\nLicense verified on the creator/distributor page on 2026-10-10. Original downloaded files are retained in `{target.as_posix()}`. Prepared effects are adaptations, not original recordings. See `Assets/Audio/SFX/Sources/Measurements.json` for exact source layers, pitch changes, cuts and offsets.\n',encoding='utf8')
    sources.append(dict(pack=name,creator=creator,url=url,license=license,licenseUrl=licenseurl,files=[dict(name=f.name,sha256=hashlib.sha256(f.read_bytes()).hexdigest()) for f in files]))

# Wind/leaf ambience, with an overlap crossfade at the original wind seam.
wind,rate=sf.read(root/'wind woosh loop.ogg',always_2d=True)
for name,pitch,leaves in [('Prismbloom',1,True),('Gloamthorne',.92,True),('Ashpetal',.85,True),('Boss',.75,False)]:
    n=round(len(wind)*RATE/rate/pitch);indices=np.arange(n)*rate*pitch/RATE
    w=np.stack([np.interp(indices,np.arange(len(wind)),wind[:,c]) for c in range(2)],axis=1)
    overlap=round(RATE*.25);period=w[:-overlap].copy()
    t=np.linspace(0,1,overlap)[:,None]
    period[:overlap]=w[-overlap:]*(1-t)+w[:overlap]*t
    bed=np.tile(period,(4,1))
    if leaves:
        l=read(leaf(3),.8); l*=.04/max(np.max(np.abs(l)),1e-10)*np.max(np.abs(bed))
        for offset,channel in [(2,0),(8,1),(14,0),(19,1)]:
            start=int(offset*RATE)
            if start+len(l)<len(bed): bed[start:start+len(l),channel]+=l
    bed*=10**(-12/20)/np.max(np.abs(bed))
    path=Path('Assets/Audio/Ambience')/(name+'_Ambience.wav');path.parent.mkdir(parents=True,exist_ok=True)
    sf.write(path,bed,RATE,subtype='PCM_16');inspect(path,[('wind woosh loop.ogg',pitch,'stereo loop overlap 250ms'),('LeavesTest001 003.flac','sparse overlay' if leaves else 'none')])
    records[-1]['seamDifference']=float(np.max(np.abs(bed[-1]-bed[0])))

(destination/'Sources'/'Measurements.json').write_text(json.dumps(dict(preparation='Trim active audio with 6ms lead/40ms tail; resample 44.1kHz; mix layers; 4ms edge fades for short effects; static peak normalization -6dBFS SFX/-12dBFS ambience; PCM16; no compression.',assets=records,sources=sources),indent=2),encoding='utf8')
Path('Tools/GameplaySfxRecipes.json').write_text(json.dumps(recipes,indent=2),encoding='utf8')
print(json.dumps(dict(prepared=len(records),effects=len(recipes),cues=len(set(r['cue'] for r in recipes)),clipped=sum(r['clippedSamples'] for r in records),seconds=round(sum(r['duration'] for r in records),2))))
