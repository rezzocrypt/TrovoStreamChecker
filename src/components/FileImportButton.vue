<script setup>
import { ref } from 'vue'
import { readFileText } from '../utils/files'

defineProps({
  label: { type: String, required: true },
  accept: { type: String, default: '.txt,text/plain' },
})

const emit = defineEmits(['import'])

const input = ref(null)

function onClick() {
  input.value?.click()
}

async function onChange(event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  const text = await readFileText(file)
  emit('import', text)
}
</script>

<template>
  <button type="button" class="btn btn-secondary" @click="onClick">
    {{ label }}
  </button>
  <input ref="input" type="file" :accept="accept" hidden @change="onChange" />
</template>