<script>
export function downloadText(filename, text) {
  const blob = new Blob([text], { type: 'text/plain;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}
</script>

<script setup>
import { ref } from 'vue'
import { platforms } from '../platforms'

defineProps({
  channels: { type: Array, required: true },
})

const emit = defineEmits(['add', 'import', 'export'])

const draft = ref('')
const platform = ref(platforms[0]?.id)
const fileInput = ref(null)

function add() {
  const value = draft.value
  draft.value = ''
  if (value.trim()) emit('add', value, platform.value)
}

function onImportClick() {
  fileInput.value?.click()
}

function readFileText(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(String(reader.result ?? ''))
    reader.onerror = () => reject(reader.error)
    reader.readAsText(file)
  })
}

async function onImportChange(event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  const text = await readFileText(file)
  emit('import', text)
}
</script>

<template>
  <section class="panel">
    <h2>Каналы</h2>

    <div class="add-row">
      <select v-model="platform">
        <option v-for="p in platforms" :key="p.id" :value="p.id">{{ p.name }}</option>
      </select>
      <input v-model="draft" type="text" placeholder="Имя канала" @keyup.enter="add" />
      <button type="button" class="btn btn-primary" :disabled="!draft.trim()" @click="add">
        Добавить
      </button>
      <button type="button" class="btn btn-secondary" title="Импорт каналов" @click="onImportClick">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
          <polyline points="7 10 12 15 17 10" />
          <line x1="12" y1="15" x2="12" y2="3" />
        </svg>
      </button>
      <input ref="fileInput" type="file" accept=".json,application/json" hidden @change="onImportChange" />
      <button
        type="button"
        class="btn btn-secondary"
        title="Экспорт каналов"
        :disabled="!channels.length"
        @click="emit('export')"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
          <polyline points="17 8 12 3 7 8" />
          <line x1="12" y1="3" x2="12" y2="15" />
        </svg>
      </button>
    </div>
  </section>
</template>