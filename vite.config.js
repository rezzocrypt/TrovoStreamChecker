import { fileURLToPath } from 'node:url'
import fs from 'node:fs'
import path from 'node:path'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

const vkProxy = {
  '/vkplay-api': {
    target: 'https://api.live.vkvideo.ru',
    changeOrigin: true,
    rewrite: (path) => path.replace(/^\/vkplay-api/, '/v1'),
  },
}

function singlefile() {
  return {
    name: 'vite:singlefile',
    enforce: 'post',
    closeBundle() {
      const dist = path.join(__dirname, 'dist')
      const htmlPath = path.join(dist, 'index.html')
      if (!fs.existsSync(htmlPath)) return

      let html = fs.readFileSync(htmlPath, 'utf8')

      const assetPath = (ref) => path.join(dist, ref.replace(/^\/+/, ''))

      html = html.replace(
        /<link rel="icon"[^>]*href="([^"]+)"[^>]*\/?>/,
        (m, href) => {
          const file = assetPath(href)
          if (!fs.existsSync(file)) return m
          const mime = path.extname(file).toLowerCase() === '.svg' ? 'image/svg+xml' : 'image/x-icon'
          const data = fs.readFileSync(file).toString('base64')
          return `<link rel="icon" type="${mime}" href="data:${mime};base64,${data}" />`
        },
      )

      html = html.replace(/<link rel="stylesheet"[^>]*href="([^"]+)"[^>]*\/?>/g, (m, href) => {
        const file = assetPath(href)
        if (!fs.existsSync(file)) return m
        return `<style>${fs.readFileSync(file, 'utf8')}</style>`
      })

      html = html.replace(/<script type="module"[^>]*src="([^"]+)"[^>]*><\/script>/g, (m, src) => {
        const file = assetPath(src)
        if (!fs.existsSync(file)) return m
        return `<script type="module">${fs.readFileSync(file, 'utf8')}</script>`
      })

      fs.writeFileSync(htmlPath, html)

      fs.rmSync(path.join(dist, 'assets'), { recursive: true, force: true })
      fs.rmSync(assetPath('/favicon.svg'), { force: true })
    },
  }
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue(), singlefile()],
  server: { proxy: vkProxy },
  preview: { proxy: vkProxy },
})