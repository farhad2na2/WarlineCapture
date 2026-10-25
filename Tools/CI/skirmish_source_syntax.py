"""Owned finite JSON/member-span syntax helpers; not gameplay or a general C# parser.

The member primitive retains the existing balanced-brace limitation. Callers
admit exact unique signatures and supported source shapes before selection.
"""
def no_duplicates(pairs):
 d={}
 for k,v in pairs:
  if k in d:raise ValueError('Duplicate JSON field: '+k)
  d[k]=v
 return d

def method_span(s,signature):
 a=s.index(signature);j=s.index('{',a);depth=1;k=j+1
 # Production methods here have balanced braces in their comments/strings; final
 # extraction proof compares every remaining byte, not a rewritten method parser.
 while depth:
  depth+=(s[k]=='{')-(s[k]=='}');k+=1
 return a,k
