"""Distinct, rhythmic original synthesized themes for forest exploration and combat."""
from pathlib import Path
import wave, json
import numpy as np
from compose_corruption_music import tone, percussion, SR

OUT=Path(__file__).resolve().parents[1]/'Assets/Audio/Music/Originals/V2'
OUT.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(302405)

def instrument(note,seconds,voice):
    t=np.arange(int(seconds*SR))/SR
    f=440*2**((note-69)/12)
    phase=2*np.pi*f*t
    env=np.minimum(1,t/.008)*np.minimum(1,(seconds-t)/.065)
    if voice=='reed':
        x=(np.sin(phase)+.32*np.sin(phase*3)+.14*np.sin(phase*5))
        x*=env*(.8+.2*np.sin(2*np.pi*4.7*t))
    elif voice=='flute':
        x=np.sin(phase+.008*np.sin(2*np.pi*5.2*t))+.18*np.sin(phase*2)+.07*np.sin(phase*3)
        x*=env*(1-.15*t/seconds)
    elif voice=='marimba':
        x=(np.sin(phase)+.3*np.sin(phase*3)*np.exp(-t*12)+.12*np.sin(phase*7)*np.exp(-t*20))*env*np.exp(-t*5)
    elif voice=='glass':
        x=(np.sin(phase+1.6*np.sin(phase*2)*np.exp(-t*6))+.12*np.sin(phase*2.76)*np.exp(-t*5))*env*np.exp(-t*3.2)
    elif voice=='brass':
        x=sum(np.sin(phase*k+.009*k*np.sin(t*2*np.pi*5))/k**1.25 for k in range(1,9) if f*k<16000)
        x*=env*(.8+.2*np.exp(-t*10))
    elif voice=='wood':
        x=(np.sin(phase)+.3*np.sin(phase*4))*env*np.exp(-t*15)
    else:
        return tone(note,seconds,voice)
    return (x*.65).astype(np.float32)

def drum(kind):
    if kind in ('kick','snare','hat'):return percussion(kind)
    seconds=.4 if kind=='tom' else .11
    t=np.arange(int(seconds*SR))/SR
    if kind=='tom':
        frequency=82+65*np.exp(-t*22)
        x=np.sin(2*np.pi*np.cumsum(frequency)/SR)*np.exp(-t*10)+.1*rng.normal(size=t.size)*np.exp(-t*30)
    else:
        noise=rng.normal(size=t.size)
        high=noise-np.roll(noise,1)
        x=.11*high*np.exp(-t*36) if kind=='shaker' else (np.sin(2*np.pi*1250*t)+.25*high)*np.exp(-t*60)*.32
    x*=np.minimum(1,t/.001)*np.minimum(1,(seconds-t)/.008)
    return x.astype(np.float32)

def render(name,bpm,meter,bars,chords,melodies,mood):
    beat=60/bpm
    length=round(bars*meter*beat*SR)
    audio=np.zeros((length,2),np.float32)
    def add(x,start,gain,pan=0):
        offset=round(start*beat*SR)%length
        x=x[:,None]*gain*np.array([np.cos((pan+1)*np.pi/4),np.sin((pan+1)*np.pi/4)],np.float32)
        at=0
        while at<len(x):
            n=min(length-offset,len(x)-at);audio[offset:offset+n]+=x[at:at+n];at+=n;offset=0
    def note(n,at,dur,voice,gain,pan=0):add(instrument(n,dur*beat,voice),at,gain,pan)
    def hit(kind,at,gain,pan=0):add(drum(kind),at,gain,pan)
    for bar in range(bars):
        chord=chords[bar%len(chords)]
        melody=melodies[bar%len(melodies)]
        at=bar*meter
        section=(bar//8)%3
        if mood=='forest':
            # A sinister compound-meter pursuit: brittle plucks and low reeds.
            for step in range(6):
                note(chord[1+step%2]+12,at+step,.7,'pluck',.19,-.38)
                hit('shaker',at+step,.25,.45)
            for step in [0,3]:
                note(chord[0],at+step,1.6,'bass',.22)
                hit('tom',at+step,.16)
            for step in [1,4]:hit('rim',at+step,.13,-.2)
            for step,n in enumerate(melody):
                note(n-12,at+step,.72,'reed',.29,.1)
                if section==1 and step in [2,5]:note(n+12,at+step,.6,'marimba',.065,-.55)
            if bar%4==3:
                note(melody[-1]-13,at+5.5,.4,'reed',.17,.1)
            if bar%2==1:
                note(chord[0]+1,at+5.5,.35,'wood',.095,-.5)
        elif mood=='roots':
            # Five-beat groove: heavier, dry and syncopated without slowing to a lament.
            for step in [0,1.5,2.5,3.5,4.5]:
                note(chord[0] if step in [0,2.5] else chord[0]+7,at+step,.6,'bass',.3)
            for step in [0,2.5,4]:hit('tom',at+step,.38,-.1)
            for step in [1,3.5]:hit('rim',at+step,.3,.3)
            for step in np.arange(0,5,.5):hit('shaker',at+step,.21,.45)
            for i,n in enumerate(melody):note(n,at+[0,.75,1.5,2.5,3.25,4.25][i],.72,'marimba',.42,-.2)
            for step in [0,2.5]:
                for n in chord[1:]:note(n,at+step,1.25,'pluck',.17,.3)
            if section==2:
                note(melody[2]-12,at+1.5,.45,'brass',.08,.12)
                note(melody[-1]-12,at+4.25,.45,'brass',.08,.12)
        elif mood=='ice':
            # Clean, quick glass arpeggios and a distinct angular lead.
            sequence=[1,2,3,2,1,3,2,1]
            for step,i in enumerate(sequence):
                note(chord[i]+12,at+step*.5,.9,'glass',.145,np.sin(step)*.65)
                hit('shaker',at+step*.5,.22,.42)
            for step in [0,2]:
                note(chord[0],at+step,1.1,'bass',.23)
                hit('kick',at+step,.22)
            for step in [1,3]:hit('rim',at+step,.2,-.25)
            for step,n in enumerate(melody):note(n,at+step*.5,.7,'glass',.235,.18)
            if section!=0:
                for n in chord[1:]:note(n,at+.25,2.8,'strings',.035,-.3)
        else:
            # Fast boss combat: sharp brass phrases, driving bass and drum fills.
            for step in range(8):
                note(chord[0]+(7 if step in [3,7] else 0),at+step*.5,.35,'bass',.31)
                note(chord[1+step%2],at+step*.5,.29,'brass',.11,-.3)
                hit('hat',at+step*.5,.22,.5)
            for step in [0,1.5,2,2.75]:hit('kick',at+step,.55)
            for step in [1,3]:hit('snare',at+step,.32,-.1)
            for step,n in enumerate(melody):note(n+(12 if section==2 else 0),at+step*.5,.43,'brass',.235,.1)
            if bar%4==3:
                for i,step in enumerate([3,3.25,3.5,3.75]):hit('tom' if i%2 else 'snare',at+step,.25,-.45+i*.28)
            if bar%8==0:
                for n in chord[1:]:note(n+12,at,1.2,'brass',.07,.4)
    dry=audio.copy()
    taps=[(.097,.10),(.173,.08),(.271,.055),(.419,.03)]
    if mood=='ice':taps += [(.587,.075),(.881,.05),(1.133,.03)]
    for delay,gain in taps:audio+=np.roll(dry[:,::-1],round(delay*SR),axis=0)*gain
    audio-=audio.mean(axis=0)
    rms_target=10**((-18 if mood!='boss' else -16.5)/20)
    audio*=min(rms_target/np.sqrt(np.mean(audio**2)),.79/np.max(np.abs(audio)))
    assert np.isfinite(audio).all() and np.max(np.abs(audio))<.8
    pcm=np.round(audio*32767).astype('<i2')
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes(pcm.tobytes())
    report={'file':name+'.wav','bpm':bpm,'beats_per_bar':meter,'bars':bars,'seconds':length/SR,'peak_dbfs':float(20*np.log10(np.max(np.abs(audio)))),'rms_dbfs':float(20*np.log10(np.sqrt(np.mean(audio**2)))),'loop_boundary_step':float(np.max(np.abs(audio[0]-audio[-1])))}
    assert report['loop_boundary_step']<.04
    print(json.dumps(report),flush=True)
    return report

tracks=[
 ('Level03_Ashpetal_FoxfireDance',156,6,24,
  [[45,52,60],[41,48,56],[47,53,62],[40,56,62]],
  [[76,77,81,80,77,76],[77,80,84,83,80,77],[77,83,86,84,83,77],[76,80,83,81,80,76]],'forest'),
 ('Level04_Veinroot_Rootwork',112,5,24,
  [[40,47,55],[41,48,56],[38,45,53],[35,42,54]],
  [[64,67,65,64,59,62],[65,68,72,71,68,65],[62,65,69,68,65,62],[63,66,71,70,66,63]],'roots'),
 ('Level05_Wraithglass_Shardrunner',124,4,24,
  [[47,54,62,65],[43,50,58,65],[48,55,63,66],[42,49,58,64]],
  [[83,78,84,86,85,81,78,83],[82,79,86,83,82,78,79,82],[84,79,87,84,83,78,79,84],[82,78,85,82,81,76,78,82]],'ice'),
 ('Boss_Dreadcrown_Crowncrusher',144,4,32,
  [[36,43,51],[32,39,48],[41,48,56],[31,38,47]],
  [[72,75,70,72,79,77,75,74],[72,75,80,79,75,72,70,68],[77,80,79,77,84,82,80,79],[74,71,74,77,79,77,74,71]],'boss'),
]
reports=[render(*track) for track in tracks]
(OUT/'Verification.json').write_text(json.dumps(reports,indent=2)+'\n')
