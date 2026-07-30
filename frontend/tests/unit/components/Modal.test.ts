import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import Modal from '@/components/Modal.vue'
import { withI18n } from '../helpers/i18n'

describe('Modal.vue', () => {
  beforeEach(() => {
    document.body.innerHTML = ''
  })

  it('does not render when open is false', () => {
    const wrapper = mount(Modal, {
      props: { open: false, title: 'Test Modal' },
      attachTo: document.body,
      ...withI18n(),
    })
    expect(document.body.querySelector('.modal-backdrop')).toBeNull()
  })

  it('renders when open is true', () => {
    mount(Modal, {
      props: { open: true, title: 'Test Modal' },
      attachTo: document.body,
      ...withI18n(),
    })
    expect(document.body.querySelector('.modal-backdrop')).not.toBeNull()
  })

  it('teleports content to body', () => {
    mount(Modal, {
      props: { open: true, title: 'Test Modal' },
      attachTo: document.body,
      ...withI18n(),
    })
    expect(document.body.querySelector('.modal')).not.toBeNull()
  })

  describe('title', () => {
    it('renders the title prop', () => {
      mount(Modal, {
        props: { open: true, title: 'Confirm Action' },
        attachTo: document.body,
        ...withI18n(),
      })
      expect(document.body.querySelector('.modal-title')?.textContent).toBe(
        'Confirm Action',
      )
    })
  })

  describe('close button', () => {
    it('renders close button with aria-label', () => {
      mount(Modal, {
        props: { open: true, title: 'Test' },
        attachTo: document.body,
        ...withI18n(),
      })
      const closeBtn = document.body.querySelector('.modal-close')
      expect(closeBtn).not.toBeNull()
      expect(closeBtn?.getAttribute('aria-label')).toBe('Close')
    })

    it('emits close when close button is clicked', async () => {
      const wrapper = mount(Modal, {
        props: { open: true, title: 'Test' },
        attachTo: document.body,
        ...withI18n(),
      })
      const closeBtn = document.body.querySelector('.modal-close') as HTMLElement
      closeBtn?.click()
      expect(wrapper.emitted('close')).toHaveLength(1)
    })
  })

  describe('backdrop click', () => {
    it('emits close when backdrop is clicked', async () => {
      const wrapper = mount(Modal, {
        props: { open: true, title: 'Test' },
        attachTo: document.body,
        ...withI18n(),
      })
      const backdrop = document.body.querySelector('.modal-backdrop') as HTMLElement
      backdrop?.click()
      expect(wrapper.emitted('close')).toHaveLength(1)
    })

    it('does not emit close when modal content is clicked', async () => {
      const wrapper = mount(Modal, {
        props: { open: true, title: 'Test' },
        attachTo: document.body,
        ...withI18n(),
      })
      const modal = document.body.querySelector('.modal') as HTMLElement
      modal?.click()
      expect(wrapper.emitted('close')).toBeUndefined()
    })
  })

  describe('slots', () => {
    it('renders default slot content', () => {
      mount(Modal, {
        props: { open: true, title: 'Test' },
        slots: { default: 'Modal body content' },
        attachTo: document.body,
        ...withI18n(),
      })
      expect(document.body.querySelector('.modal-body')?.textContent).toContain(
        'Modal body content',
      )
    })

    it('renders footer slot when provided', () => {
      mount(Modal, {
        props: { open: true, title: 'Test' },
        slots: {
          default: 'Body',
          footer: '<button>Confirm</button>',
        },
        attachTo: document.body,
        ...withI18n(),
      })
      expect(document.body.querySelector('.modal-footer')).not.toBeNull()
      expect(document.body.querySelector('.modal-footer')?.textContent).toContain(
        'Confirm',
      )
    })

    it('does not render footer when no footer slot', () => {
      mount(Modal, {
        props: { open: true, title: 'Test' },
        slots: { default: 'Body' },
        attachTo: document.body,
        ...withI18n(),
      })
      expect(document.body.querySelector('.modal-footer')).toBeNull()
    })
  })

  describe('accessibility', () => {
    it('has role="dialog" on modal element', () => {
      mount(Modal, {
        props: { open: true, title: 'Test' },
        attachTo: document.body,
        ...withI18n(),
      })
      expect(document.body.querySelector('.modal')?.getAttribute('role')).toBe(
        'dialog',
      )
    })

    it('has aria-modal="true"', () => {
      mount(Modal, {
        props: { open: true, title: 'Test' },
        attachTo: document.body,
        ...withI18n(),
      })
      expect(document.body.querySelector('.modal')?.getAttribute('aria-modal')).toBe(
        'true',
      )
    })
  })
})
