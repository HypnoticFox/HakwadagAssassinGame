import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'

import LoadingSpinner from '@/components/LoadingSpinner.vue'

describe('LoadingSpinner.vue', () => {
  it('renders a loading screen with a spinner', () => {
    const wrapper = mount(LoadingSpinner)

    expect(wrapper.find('.loading-screen').exists()).toBe(true)
    expect(wrapper.find('.loading-screen .spinner').exists()).toBe(true)
  })

  it('does not apply the inline class by default', () => {
    const wrapper = mount(LoadingSpinner)

    expect(wrapper.classes()).toContain('loading-screen')
    expect(wrapper.classes()).not.toContain('loading-screen--inline')
  })

  it('applies the inline class when requested', () => {
    const wrapper = mount(LoadingSpinner, { props: { inline: true } })

    expect(wrapper.classes()).toContain('loading-screen--inline')
  })

  it('has a status role', () => {
    const wrapper = mount(LoadingSpinner)

    expect(wrapper.attributes('role')).toBe('status')
  })
})
