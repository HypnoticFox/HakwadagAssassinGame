import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ConditionCard from '@/components/ConditionCard.vue'
import { ConditionType } from '@/types'
import { withI18n } from '../helpers/i18n'

describe('ConditionCard.vue', () => {
  describe('display text', () => {
    it('shows description when provided', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.Custom,
            description: 'Must be holding a coffee cup',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('Must be holding a coffee cup')
    })

    it('shows auto-generated text for WithSpecificPerson', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.WithSpecificPerson,
            description: '',
            targetPersonName: 'Bob',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('With Bob')
    })

    it('shows resolved player name instead of description with GUID', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.WithSpecificPerson,
            description: 'With specific person (b7a3c9f0-4d1e-4a5b-9c2d-8e6f1a0b3c4d)',
            targetPersonName: 'Bob',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('With Bob')
      expect(wrapper.text()).not.toContain('b7a3c9f0')
    })

    it('shows fallback for WithSpecificPerson without name', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.WithSpecificPerson,
            description: '',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('With a specific person')
    })

    it('shows auto-generated text for Alone', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.Alone,
            description: '',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('Target is alone')
    })

    it('shows auto-generated text for WithXPeople', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.WithXPeople,
            description: '',
            minPeople: 3,
          },
        },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('With at least 3 other people')
    })

    it('shows auto-generated text for WithXPeople without minPeople', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.WithXPeople,
            description: '',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('With at least 2 other people')
    })

    it('shows auto-generated text for MundaneAction', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.MundaneAction,
            description: '',
            action: 'eating',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('While target is eating')
    })

    it('shows fallback for MundaneAction without action', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.MundaneAction,
            description: '',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('While target is doing something')
    })

    it('shows fallback for Custom without description', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.Custom,
            description: '',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('Custom condition')
    })
  })

  describe('type label', () => {
    it('shows type label for custom condition', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.Custom,
            description: 'Something custom',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.find('.condition-card__type').text()).toBe('Custom')
    })

    it('shows type label for alone condition', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c2',
            type: ConditionType.Alone,
            description: '',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.find('.condition-card__type').text()).toBe('Alone')
    })
  })

  describe('selectable mode', () => {
    it('has condition-card--selectable class when selectable', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.Custom,
            description: 'Do something',
          },
          selectable: true,
        },
        ...withI18n(),
      })
      expect(wrapper.classes()).toContain('condition-card--selectable')
    })

    it('does not have selectable class by default', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.Custom,
            description: 'Do something',
          },
        },
        ...withI18n(),
      })
      expect(wrapper.classes()).not.toContain('condition-card--selectable')
    })

    it('has selected class when selected', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.Custom,
            description: 'Do something',
          },
          selectable: true,
          selected: true,
        },
        ...withI18n(),
      })
      expect(wrapper.classes()).toContain('condition-card--selected')
    })

    it('does not have selected class when not selected', () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.Custom,
            description: 'Do something',
          },
          selectable: true,
          selected: false,
        },
        ...withI18n(),
      })
      expect(wrapper.classes()).not.toContain('condition-card--selected')
    })
  })

  describe('select event', () => {
    it('emits select with condition id when selectable and clicked', async () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.Custom,
            description: 'Do something',
          },
          selectable: true,
        },
        ...withI18n(),
      })
      await wrapper.trigger('click')
      expect(wrapper.emitted('select')).toHaveLength(1)
      expect(wrapper.emitted('select')![0]).toEqual(['c1'])
    })

    it('does not emit select when not selectable', async () => {
      const wrapper = mount(ConditionCard, {
        props: {
          condition: {
            id: 'c1',
            type: ConditionType.Custom,
            description: 'Do something',
          },
        },
        ...withI18n(),
      })
      await wrapper.trigger('click')
      expect(wrapper.emitted('select')).toBeUndefined()
    })
  })
})
