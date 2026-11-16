require 'yaml'
require 'json'
def check(node)
  if node.is_a?(Psych::Nodes::Alias)
    raise 'YAML aliases unsupported'
  elsif node.is_a?(Psych::Nodes::Mapping)
    keys=[]
    node.children.each_slice(2) do |key,value|
      raise 'Only scalar YAML keys admitted' unless key.is_a?(Psych::Nodes::Scalar)
      raise 'Duplicate YAML key' if keys.include?(key.value)
      keys << key.value
      check(value)
    end
  elsif node.respond_to?(:children) && node.children
    node.children.each{|child|check(child)}
  end
end
def spellings(node,path='',output={})
 if node.is_a?(Psych::Nodes::Mapping)
  node.children.each_slice(2){|key,value|spellings(value,path.empty? ? key.value : path+'.'+key.value,output)}
 elsif node.is_a?(Psych::Nodes::Sequence)
  node.children.each_with_index{|value,index|spellings(value,path+'['+index.to_s+']',output)}
 elsif node.is_a?(Psych::Nodes::Scalar)
  output[path]=node.value
 elsif node.respond_to?(:children) && node.children
  node.children.each{|child|spellings(child,path,output)}
 end
 output
end
rows={}
ARGV.each do |path|
  raw=File.binread(path)
  raise 'Bounded source config required' unless raw.bytesize>0 && raw.bytesize<=2097152
  text=raw.lines.reject{|x|x.start_with?('%')}.join.gsub(/^--- !u!\d+ &-?\d+$/, '---')
  tree=Psych.parse_stream(text)
  raise 'Exactly one config document required' unless tree.children.length==1
  check(tree)
  rows[path]=YAML.safe_load(text, permitted_classes: [], permitted_symbols: [], aliases: false)
  rows[path]["__source_scalar_spellings"]=spellings(tree)
end
STDOUT.write(JSON.generate(rows))
