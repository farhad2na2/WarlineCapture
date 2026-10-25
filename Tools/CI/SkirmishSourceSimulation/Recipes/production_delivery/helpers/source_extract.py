import re
def block(s,start):
 at=s.index('{',start);depth=1;i=at+1;state='code'
 while depth:
  if i>=len(s):raise ValueError('Unclosed source block')
  c=s[i];n=s[i:i+2]
  if state=='line':
   if c=='\n':state='code'
  elif state=='comment':
   if n=='*/':state='code';i+=1
  elif state in ('string','char'):
   if c=='\\':i+=1
   elif c==('"' if state=='string' else "'"):state='code'
  elif n=='//':state='line';i+=1
  elif n=='/*':state='comment';i+=1
  elif c=='"':state='string'
  elif c=="'":state='char'
  elif c=='{':depth+=1
  elif c=='}':depth-=1
  i+=1
 return i
def method(text,name,occurrence=0):
 pattern=r'^[ \t]*(?:public|private|internal|protected)\s+(?:static\s+)?[^\n;={}]+?\b'+re.escape(name)+r'(?:<[^\n>]+>)?\s*\('
 matches=list(re.finditer(pattern,text,re.M))
 if occurrence>=len(matches):raise ValueError('Missing current source member: '+name)
 m=matches[occurrence];end=block(text,m.start())
 return text[m.start():end],{'name':name,'start_line':text.count('\n',0,m.start())+1,'end_line':text.count('\n',0,end)+1}
